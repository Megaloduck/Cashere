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
    private decimal _cartSubtotal;

    [ObservableProperty]
    private CameraPermissionStatus _cameraPermission = CameraPermissionStatus.Unknown;

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool ShowCameraPreview => IsConnected && CameraPermission == CameraPermissionStatus.Granted;
    public bool CameraPermissionDenied => CameraPermission == CameraPermissionStatus.Denied;

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

    private async void HandleSyncStateChanged(SyncConnectionState state)
    {
        State = state;

        // Release the camera the moment the connection drops, regardless of
        // whether the disconnect happened from this tab or the Pairing tab.
        if (state != SyncConnectionState.Connected && _scanner is not null)
        {
            await _scanner.StopAsync();
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

    partial void OnStateChanged(SyncConnectionState value)
    {
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ShowCameraPreview));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    partial void OnCameraPermissionChanged(CameraPermissionStatus value)
    {
        OnPropertyChanged(nameof(ShowCameraPreview));
        OnPropertyChanged(nameof(CameraPermissionDenied));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    [RelayCommand]
    private async Task EnableCamera()
    {
        if (_scanner is null) return;

        CameraPermission = await _scanner.RequestCameraPermissionAsync();
    }

    [RelayCommand]
    private async Task SendManualScan()
    {
        if (string.IsNullOrWhiteSpace(ManualBarcode)) return;

        var barcode = ManualBarcode.Trim();
        ManualBarcode = string.Empty;
        await ProcessScanAsync(barcode);
    }

    // Shared by both the manual TextBox path and the camera path above - the
    // ScanBarcode round trip to the till is identical either way.
    private async Task ProcessScanAsync(string barcode)
    {
        try
        {
            var outcome = await _syncClient.ScanBarcodeAsync(barcode);
            LastScanMessage = outcome.Found
                ? $"Added: {outcome.ProductName}"
                : outcome.Message ?? "No product matches that barcode.";

            if (outcome.Cart is not null)
            {
                HandleCartUpdated(outcome.Cart);
            }
        }
        catch (Exception ex)
        {
            LastScanMessage = $"Scan failed: {ex.Message}";
        }
    }
}