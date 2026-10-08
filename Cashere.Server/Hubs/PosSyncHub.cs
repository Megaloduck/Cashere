using Cashere.Data;
using Cashere.Models;
using Cashere.Server.Services;
using Cashere.Sync.Dtos;
using Cashere.Sync.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Cashere.Server.Hubs;

public class PosSyncHub : Hub<IPosSyncClient>, IPosSyncHub
{
    private readonly CashereDbContext _db;
    private readonly ActiveCartService _cart;
    private readonly ConnectedDeviceService _devices;

    public PosSyncHub(CashereDbContext db, ActiveCartService cart, ConnectedDeviceService devices)
    {
        _db = db;
        _cart = cart;
        _devices = devices;
    }

    // Registers this connection as a "connected device" the desktop admin
    // Devices screen can list. DeviceName is read from the ?deviceName=
    // query string the mobile client attaches when building its
    // HubConnection (see SignalRPosSyncClientService.ConnectAsync);
    // IpAddress comes straight off the underlying HTTP connection so it
    // can't be spoofed by whatever a client happens to send.
    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var deviceName = httpContext?.Request.Query["deviceName"].ToString();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        _devices.RegisterConnected(Context.ConnectionId, deviceName, ipAddress);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _devices.RegisterDisconnected(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    // Called by the mobile client after a camera scan. Looks the barcode up
    // against the local catalog, adds it to the shared active cart, and
    // pushes the updated cart to every connected client (desktop + mobile).
    public async Task<ScanResultDto> ScanBarcode(ScanBarcodeRequest request)
    {
        var scanValue = request.Barcode?.Trim();
        var product = ProductQrIdentity.TryGetProductId(scanValue, out var productId)
            ? await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive)
            : await _db.Products.FirstOrDefaultAsync(p => p.Barcode == scanValue && p.IsActive);

        if (product is null)
        {
            return new ScanResultDto(false, null, null, 0, "No product matches that barcode or Cashere QR identity.");
        }

        return new ScanResultDto(true, product.Id, product.Name, product.SellingPrice, null);
    }

    public async Task<CartMutationResultDto> AddProductToCart(int productId, int quantity)
    {
        if (quantity <= 0)
            return new CartMutationResultDto(false, "Quantity must be greater than zero.", _cart.GetCart());

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
        if (product is null)
            return new CartMutationResultDto(false, "That product is no longer active.", _cart.GetCart());

        var cart = _cart.AddItem(product.Id, product.Name, product.SellingPrice, quantity);
        await Clients.All.CartUpdated(cart);
        return new CartMutationResultDto(true, null, cart);
    }

    public async Task<CartMutationResultDto> DecrementCartItem(int productId)
    {
        var cart = _cart.DecrementItem(productId);
        await Clients.All.CartUpdated(cart);
        return new CartMutationResultDto(true, null, cart);
    }

    public async Task<CartMutationResultDto> RemoveCartItem(int productId)
    {
        var cart = _cart.RemoveItem(productId);
        await Clients.All.CartUpdated(cart);
        return new CartMutationResultDto(true, null, cart);
    }

    public async Task<CartMutationResultDto> ClearCart()
    {
        var cart = _cart.Clear();
        await Clients.All.CartUpdated(cart);
        return new CartMutationResultDto(true, null, cart);
    }

    public Task<CartDto> GetCurrentCart() => Task.FromResult(_cart.GetCart());

    public async Task<PaymentOptionsDto> GetPaymentOptions()
    {
        var settings = await _db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();
        return new PaymentOptionsDto(
            settings?.CashEnabled ?? true,
            settings?.QrisEnabled ?? true,
            settings?.EdcEnabled ?? true,
            settings?.QrisAccountInfo,
            settings?.EdcAccountInfo);
    }
}
