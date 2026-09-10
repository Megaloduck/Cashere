using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using Cashere.Services;
using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Avalonia.Threading;

namespace Cashere.Android.Services;

public class SignalRPosSyncClientService : IPosSyncClientService
{
    private const string LastEndpointFileName = "last-shop-endpoint.txt";

    private readonly HttpClient _httpClient = new();
    private HubConnection? _connection;

    public SyncConnectionState State { get; private set; } = SyncConnectionState.Disconnected;
    public ShopEndpoint? CurrentEndpoint { get; private set; }
    public string? ShopName { get; private set; }

    public event Action<SyncConnectionState>? StateChanged;
    public event Action<SyncCartSnapshot>? CartUpdated;
    public event Action? ProductCatalogChanged;
    public event Action<string>? Kicked;

    public async Task<PairingResult> ConnectAsync(ShopEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();

        SetState(SyncConnectionState.Connecting);

        var baseAddress = $"http://{endpoint.Host}:{endpoint.Port}";

        try
        {
            var health = await _httpClient.GetFromJsonAsync<HealthResponse>(
                $"{baseAddress}/api/health", cancellationToken);

            if (health is null)
            {
                SetState(SyncConnectionState.Failed);
                return new PairingResult(false, null, "The till didn't respond as expected.");
            }

            // Picked up server-side by PosSyncHub.OnConnectedAsync and shown
            // on the desktop admin's Devices screen - purely informational,
            // the hub never trusts it for anything security-relevant.
            var deviceName = Uri.EscapeDataString(GetDeviceName());

            _connection = new HubConnectionBuilder()
                .WithUrl($"{baseAddress}/hubs/pos-sync?deviceName={deviceName}")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<CartDto>("CartUpdated", dto => CartUpdated?.Invoke(MapCart(dto)));

            _connection.On("ProductCatalogChanged", () =>
                Dispatcher.UIThread.Post(() => ProductCatalogChanged?.Invoke()));

            // Told by the till's admin Devices screen to disconnect. Offloaded
            // via Task.Run rather than awaited inline: this handler runs on
            // the connection's own receive loop, and DisconnectAsync() below
            // disposes that same connection - awaiting it synchronously here
            // would deadlock waiting for a loop it's currently blocking.
            _connection.On<string>("Kicked", reason =>
            {
                _ = Task.Run(async () =>
                {
                    await DisconnectAsync();
                    Dispatcher.UIThread.Post(() => Kicked?.Invoke(reason));
                });
            });

            _connection.Reconnecting += _ => { SetState(SyncConnectionState.Reconnecting); return Task.CompletedTask; };
            _connection.Reconnected += _ => { SetState(SyncConnectionState.Connected); return Task.CompletedTask; };
            _connection.Closed += _ => { SetState(SyncConnectionState.Disconnected); return Task.CompletedTask; };

            await _connection.StartAsync(cancellationToken);

            CurrentEndpoint = endpoint;
            ShopName = health.ShopName;
            SetState(SyncConnectionState.Connected);

            await SaveLastEndpointAsync(endpoint);

            return new PairingResult(true, health.ShopName, null);
        }
        catch (Exception ex)
        {
            SetState(SyncConnectionState.Failed);
            return new PairingResult(false, null, ex.Message);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        CurrentEndpoint = null;
        ShopName = null;
        SetState(SyncConnectionState.Disconnected);
    }

    public async Task<BarcodeScanOutcome> ScanBarcodeAsync(string barcode, int quantity = 1)
    {
        if (_connection is null)
        {
            return new BarcodeScanOutcome(false, null, "Not connected to a till.", null);
        }

        var result = await _connection.InvokeAsync<ScanResultDto>(
            "ScanBarcode", new ScanBarcodeRequest(barcode, quantity));

        return new BarcodeScanOutcome(
            result.Found, result.ProductName, result.Message,
            result.Cart is null ? null : MapCart(result.Cart));
    }

    public async Task<SyncCartSnapshot> GetCurrentCartAsync()
    {
        if (_connection is null)
        {
            return new SyncCartSnapshot(Array.Empty<SyncCartLine>(), 0, DateTime.UtcNow);
        }

        var cart = await _connection.InvokeAsync<CartDto>("GetCurrentCart");
        return MapCart(cart);
    }

    public async Task<ShopEndpoint?> LoadLastEndpointAsync()
    {
        try
        {
            var path = GetLastEndpointPath();
            if (!File.Exists(path)) return null;

            var text = await File.ReadAllTextAsync(path);
            var parts = text.Split(':', 2);
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port)) return null;

            return new ShopEndpoint(parts[0], port);
        }
        catch
        {
            return null;
        }
    }

    private static async Task SaveLastEndpointAsync(ShopEndpoint endpoint)
    {
        try
        {
            await File.WriteAllTextAsync(GetLastEndpointPath(), $"{endpoint.Host}:{endpoint.Port}");
        }
        catch
        {
        }
    }

    private static string GetLastEndpointPath()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(folder, LastEndpointFileName);
    }

    // Best-effort friendly label for the desktop admin's Devices screen -
    // e.g. "Google Pixel 7". Never throws - a device-name failure shouldn't
    // break pairing.
    private static string GetDeviceName()
    {
        try
        {
            var manufacturer = global::Android.OS.Build.Manufacturer ?? string.Empty;
            var model = global::Android.OS.Build.Model ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model))
            {
                return "Android Device";
            }

            return string.IsNullOrWhiteSpace(manufacturer) || model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase)
                ? model
                : $"{manufacturer} {model}";
        }
        catch
        {
            return "Android Device";
        }
    }

    private void SetState(SyncConnectionState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }

    private static SyncCartSnapshot MapCart(CartDto dto) => new(
        dto.Items.Select(i => new SyncCartLine(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal)).ToList(),
        dto.Subtotal,
        dto.UpdatedAt);

    private record HealthResponse(string ShopName, string Status);
}