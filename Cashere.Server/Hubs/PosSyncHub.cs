using Cashere.Data;
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

    public PosSyncHub(CashereDbContext db, ActiveCartService cart)
    {
        _db = db;
        _cart = cart;
    }

    // Called by the mobile client after a camera scan. Looks the barcode up
    // against the local catalog, adds it to the shared active cart, and
    // pushes the updated cart to every connected client (desktop + mobile).
    public async Task<ScanResultDto> ScanBarcode(ScanBarcodeRequest request)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Barcode == request.Barcode && p.IsActive);

        if (product is null)
        {
            return new ScanResultDto(false, null, "No product matches that barcode.", null);
        }

        var cart = _cart.AddItem(product.Id, product.Name, product.SellingPrice, request.Quantity);
        await Clients.All.CartUpdated(cart);

        return new ScanResultDto(true, product.Name, null, cart);
    }

    public Task<CartDto> GetCurrentCart() => Task.FromResult(_cart.GetCart());
}
