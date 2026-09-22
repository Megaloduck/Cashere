using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Mobile;

// Camera + manual barcode scanning and the live shared cart - split out of
// the old combined PairingViewModel once the mobile UI grew a sidebar.
// Stays subscribed to the shared sync client for its whole lifetime (it's
// owned by MobileShellViewModel, which lives as long as the app does), so
// cart/connection state stays correct even while a different tab is visible.
public partial class ScanningViewModel : ViewModelBase
{
    private readonly IPosSyncClientService _syncClient;
    private readonly IBarcodeScannerService? _scanner;

    // Exposed so the View's code-behind can create/host the native preview
    // control - Views are allowed to know about platform Controls, ViewModels
    // aren't, so the control itself is never bound directly.
    public IBarcodeScannerService? Scanner => _scanner;

    public ObservableCollection<SyncCartLine> CartLines { get; } = new();

    // Raised whenever ShowCameraPreview flips, so the View knows to
    // attach/detach the native preview control.
    public event Action<bool>? CameraReadyChanged;

    [ObservableProperty]
    private SyncConnectionState _state = SyncConnectionState.Disconnected;

    [ObservableProperty]
    private string _manualBarcode = string.Empty;

    [ObservableProperty]
    private string _lastScanMessage = string.Empty;

    [ObservableProperty]
    private bool _isCameraActive;

    [ObservableProperty]
    private CameraPermissionStatus _cameraPermission = CameraPermissionStatus.Unknown;

    [ObservableProperty]
    private decimal _cartSubtotal;

    [ObservableProperty]
    private int _scanQuantity = 1;

    // ---- Derived state -----------------------------------------------------

    public bool IsConnected => State == SyncConnectionState.Connected;

    // Single source of truth: preview is only shown when we're connected,
    // permission is granted, and the scanner session is actually running.
    public bool ShowCameraPreview =>
        IsConnected &&
        CameraPermission == CameraPermissionStatus.Granted &&
        IsCameraActive;

    public bool CameraPermissionDenied =>
        CameraPermission == CameraPermissionStatus.Denied;

    // ---- Commands ----------------------------------------------------------

    [RelayCommand]
    private void IncrementQuantity() => ScanQuantity++;

    [RelayCommand]
    private void DecrementQuantity()
    {
        if (ScanQuantity > 1) ScanQuantity--;
    }

    [RelayCommand]
    private void QuickQuantity1() => ScanQuantity = 1;

    [RelayCommand]
    private void QuickQuantity5() => ScanQuantity = 5;

    [RelayCommand]
    private void QuickQuantity10() => ScanQuantity = 10;

    [RelayCommand]
    private async Task EnableCamera()
    {
        if (_scanner is null) return;

        CameraPermission = await _scanner.RequestCameraPermissionAsync();
        if (CameraPermission == CameraPermissionStatus.Granted)
        {
            IsCameraActive = true;
        }
    }

    [RelayCommand]
    private async Task SendManualScan()
    {
        if (string.IsNullOrWhiteSpace(ManualBarcode)) return;

        var barcode = ManualBarcode.Trim();
        ManualBarcode = string.Empty;
        await ProcessScanAsync(barcode);
    }

    // ---- Construction ------------------------------------------------------

    public ScanningViewModel(IPosSyncClientService syncClient, IBarcodeScannerService? scanner = null)
    {
        _syncClient = syncClient;
        _scanner = scanner;
        _syncClient.StateChanged += HandleSyncStateChanged;
        _syncClient.CartUpdated += HandleCartUpdated;

        if (_scanner is not null)
        {
            _scanner.BarcodeScanned += HandleBarcodeScanned;
            CameraPermission = _scanner.PermissionStatus;
        }

        State = _syncClient.State;
    }

    // ---- Lifecycle ---------------------------------------------------------

    // Called by MobileShellViewModel whenever this section becomes visible.
    public async Task RefreshAsync()
    {
        if (!IsConnected) return;

        try
        {
            var cart = await _syncClient.GetCurrentCartAsync();
            HandleCartUpdated(cart);
        }
        catch
        {
            // Best-effort refresh - a live CartUpdated push will catch up if this fails.
        }
    }

    // Called by MobileShellViewModel whenever this tab is navigated away from.
    // Tears the camera down right now and flips IsCameraActive false, so the
    // next visit requires (and correctly triggers) a fresh StartAsync() against
    // whatever new View instance is showing - instead of silently believing the
    // camera is "already on" while the session backing that belief was actually
    // torn down along with the old View.
    public async Task DeactivateCameraAsync()
    {
        if (!IsCameraActive) return;

        IsCameraActive = false;

        if (_scanner is not null)
        {
            await _scanner.StopAsync();
        }
    }

    // ---- Event handlers ----------------------------------------------------

    private async void HandleSyncStateChanged(SyncConnectionState state)
    {
        State = state;

        // Release the camera the moment the connection drops, regardless of
        // whether the disconnect happened from this tab or the Pairing tab.
        if (state != SyncConnectionState.Connected && _scanner is not null)
        {
            await _scanner.StopAsync();
            IsCameraActive = false;
        }
    }

    private void HandleCartUpdated(SyncCartSnapshot cart)
    {
        CartLines.Clear();
        foreach (var line in cart.Items)
        {
            CartLines.Add(line);
        }
        CartSubtotal = cart.Subtotal;
    }

    // Camera path feeds into the exact same scan handling as manual entry -
    // the till has no way to tell the two apart, by design.
    private void HandleBarcodeScanned(string barcode) => _ = ProcessScanAsync(barcode);

    // ---- Property change hooks --------------------------------------------

    partial void OnStateChanged(SyncConnectionState value)
    {
        OnPropertyChanged(nameof(IsConnected));
        NotifyCameraPreviewChanged();
    }

    partial void OnCameraPermissionChanged(CameraPermissionStatus value)
    {
        OnPropertyChanged(nameof(CameraPermissionDenied));
        NotifyCameraPreviewChanged();
    }

    partial void OnIsCameraActiveChanged(bool value) => NotifyCameraPreviewChanged();

    private void NotifyCameraPreviewChanged()
    {
        OnPropertyChanged(nameof(ShowCameraPreview));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    // ---- Scan processing ---------------------------------------------------

    // Shared by both the manual TextBox path and the camera path above - the
    // ScanBarcode round trip to the till is identical either way.
    private async Task ProcessScanAsync(string barcode)
    {
        var quantity = ScanQuantity;
        try
        {
            var outcome = await _syncClient.ScanBarcodeAsync(barcode, quantity);
            LastScanMessage = outcome.Found
                ? $"Added: {outcome.ProductName} x{quantity}"
                : outcome.Message ?? "No product matches that barcode.";

            if (outcome.Cart is not null)
            {
                HandleCartUpdated(outcome.Cart);
            }

            if (outcome.Found)
            {
                ScanQuantity = 1;
            }
        }
        catch (Exception ex)
        {
            LastScanMessage = $"Scan failed: {ex.Message}";
        }
    }
}