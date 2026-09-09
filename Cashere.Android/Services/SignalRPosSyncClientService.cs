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

// SignalR-backed implementation of IPosSyncClientService. Lives in
// Cashere.Android (not the shared, EF-free Cashere project) so it's the only
// place carrying the SignalR client + networking dependencies - same
// separation as EF-backed services living in Cashere.Data instead of Cashere.
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

    public async Task<PairingResult> ConnectAsync(ShopEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();

        SetState(SyncConnectionState.Connecting);

        var baseAddress = $"http://{endpoint.Host}:{endpoint.Port}";

        try
        {
            // Confirm this is actually a Cashere till before opening a hub
            // connection - turns a typo'd IP into a clear message instead of
            // a raw SignalR negotiation failure.
            var health = await _httpClient.GetFromJsonAsync<HealthResponse>(
                $"{baseAddress}/api/health", cancellationToken);

            if (health is null)
            {
                SetState(SyncConnectionState.Failed);
                return new PairingResult(false, null, "The till didn't respond as expected.");
            }

            _connection = new HubConnectionBuilder()
                .WithUrl($"{baseAddress}/hubs/pos-sync")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<CartDto>("CartUpdated", dto => CartUpdated?.Invoke(MapCart(dto)));

            // Dispatched onto the UI thread since subscribers (e.g.
            // LabelingViewModel) mutate ObservableCollections in response,
            // and this callback otherwise runs on a SignalR threadpool thread.
            _connection.On("ProductCatalogChanged", () =>
                Dispatcher.UIThread.Post(() => ProductCatalogChanged?.Invoke()));

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
            // Best-effort convenience feature - a missing/corrupt file just
            // means the cashier types the address once.
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
            // Non-critical - worst case the cashier re-enters the address next time.
        }
    }

    private static string GetLastEndpointPath()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(folder, LastEndpointFileName);
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