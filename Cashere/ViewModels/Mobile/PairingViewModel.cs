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

public partial class PairingViewModel : ViewModelBase
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
    private string _hostInput = string.Empty;

    [ObservableProperty]
    private string _portInput = "5177";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private SyncConnectionState _state = SyncConnectionState.Disconnected;

    [ObservableProperty]
    private string? _shopName;

    [ObservableProperty]
    private string _manualBarcode = string.Empty;

    [ObservableProperty]
    private string _lastScanMessage = string.Empty;

    [ObservableProperty]
    private decimal _cartSubtotal;

    [ObservableProperty]
    private CameraPermissionStatus _cameraPermission = CameraPermissionStatus.Unknown;

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool CanConnect => !IsBusy && State != SyncConnectionState.Connected && State != SyncConnectionState.Connecting;
    public bool ShowCameraPreview => IsConnected && CameraPermission == CameraPermissionStatus.Granted;
    public bool CameraPermissionDenied => CameraPermission == CameraPermissionStatus.Denied;

    public PairingViewModel(IPosSyncClientService syncClient, IBarcodeScannerService? scanner = null)
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
    }

    public async Task InitializeAsync()
    {
        var last = await _syncClient.LoadLastEndpointAsync();
        if (last is not null)
        {
            HostInput = last.Host;
            PortInput = last.Port.ToString();
        }
    }

    private void HandleSyncStateChanged(SyncConnectionState state) => State = state;

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
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(ShowCameraPreview));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanConnect));

    partial void OnCameraPermissionChanged(CameraPermissionStatus value)
    {
        OnPropertyChanged(nameof(ShowCameraPreview));
        OnPropertyChanged(nameof(CameraPermissionDenied));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    [RelayCommand]
    private async Task Connect()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(HostInput))
        {
            ErrorMessage = "Enter the till's IP address.";
            return;
        }

        if (!int.TryParse(PortInput, out var port))
        {
            ErrorMessage = "Enter a valid port number.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _syncClient.ConnectAsync(new ShopEndpoint(HostInput.Trim(), port));
            if (result.Success)
            {
                ShopName = result.ShopName;
                var cart = await _syncClient.GetCurrentCartAsync();
                HandleCartUpdated(cart);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Could not connect. Check the address and try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        if (_scanner is not null)
        {
            await _scanner.StopAsync();
        }

        await _syncClient.DisconnectAsync();
        ShopName = null;
        CartLines.Clear();
        CartSubtotal = 0;
        LastScanMessage = string.Empty;
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