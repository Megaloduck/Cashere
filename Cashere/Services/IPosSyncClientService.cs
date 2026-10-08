using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Cashere.Services;

public enum SyncConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}

public record ShopEndpoint(string Host, int Port);

public record PairingResult(bool Success, string? ShopName, string? ErrorMessage);

public record SyncCartLine(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public record SyncCartSnapshot(IReadOnlyList<SyncCartLine> Items, decimal Subtotal, DateTime UpdatedAt);

public record BarcodeScanOutcome(bool Found, int? ProductId, string? ProductName, decimal UnitPrice, string? Message);

public record CartMutationOutcome(bool Success, string? Message, SyncCartSnapshot Cart);

public record SyncPaymentOptions(bool CashEnabled, bool QrisEnabled, bool EdcEnabled, string? QrisAccountInfo, string? EdcAccountInfo);

public record SyncPaymentNotification(string SaleNumber, decimal Amount, DateTime PaidAt);

// Implemented in Cashere.Android using the SignalR client - this interface just
// describes the contract, the same way ISaleService describes a contract that
// Cashere.Data fulfils with EF Core. Keeps the shared project free of the
// SignalR/networking dependency.
public interface IPosSyncClientService
{
    SyncConnectionState State { get; }
    ShopEndpoint? CurrentEndpoint { get; }
    string? ShopName { get; }

    event Action<SyncConnectionState>? StateChanged;
    event Action<SyncCartSnapshot>? CartUpdated;
    event Action<SyncPaymentNotification>? PaymentNotificationReceived;

    // Pushed by the till whenever an admin-driven change (product create/edit,
    // activate/deactivate, photo upload/removal, purchase stock update) makes
    // the mobile catalog projection stale. LabelingViewModel subscribes to
    // this to auto-refresh its product list.
    event Action? ProductCatalogChanged;

    // Raised after this device has been kicked from the till's Devices
    // screen and has already disconnected itself. Carries the reason text
    // the till sent, purely for display - subscribers don't need to tear
    // anything down themselves, that already happened.
    event Action<string>? Kicked;

    Task<PairingResult> ConnectAsync(ShopEndpoint endpoint, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<BarcodeScanOutcome> ScanBarcodeAsync(string barcode);
    Task<CartMutationOutcome> AddProductToCartAsync(int productId, int quantity);
    Task<CartMutationOutcome> DecrementCartItemAsync(int productId);
    Task<CartMutationOutcome> RemoveCartItemAsync(int productId);
    Task<CartMutationOutcome> ClearCartAsync();
    Task<SyncCartSnapshot> GetCurrentCartAsync();
    Task<SyncPaymentOptions> GetPaymentOptionsAsync();

    // Lets the pairing screen pre-fill the last shop this device connected to.
    Task<ShopEndpoint?> LoadLastEndpointAsync();
}
