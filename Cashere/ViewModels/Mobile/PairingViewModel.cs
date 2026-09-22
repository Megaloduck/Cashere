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

    [ObservableProperty]
    private string _hostInput = string.Empty;

    [ObservableProperty]
    private string _portInput = "5177";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private CameraPermissionStatus _cameraPermission = CameraPermissionStatus.Unknown;

    [ObservableProperty]
    private SyncConnectionState _state = SyncConnectionState.Disconnected;

    [ObservableProperty]
    private string? _shopName;

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool CanConnect => !IsBusy && State != SyncConnectionState.Connected && State != SyncConnectionState.Connecting;

    public PairingViewModel(IPosSyncClientService syncClient)
    {
        _syncClient = syncClient;
        _syncClient.StateChanged += HandleSyncStateChanged;
        _syncClient.Kicked += HandleKicked;
    }

    public async Task InitializeAsync()
    {
        var last = await _syncClient.LoadLastEndpointAsync();
        if (last is not null)
        {
            HostInput = last.Host;
            PortInput = last.Port.ToString();
        }

        State = _syncClient.State;
        ShopName = _syncClient.ShopName;
    }

    private void HandleSyncStateChanged(SyncConnectionState state) => State = state;

    // By the time this fires the client has already disconnected (see
    // SignalRPosSyncClientService) - just surface why, on the screen the
    // cashier would naturally look at to reconnect.
    private void HandleKicked(string reason)
    {
        ShopName = null;
        ErrorMessage = $"Disconnected by the till: {reason}";
    }

    partial void OnStateChanged(SyncConnectionState value)
    {
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(CanConnect));
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanConnect));

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
        await _syncClient.DisconnectAsync();
        ShopName = null;
    }

    private readonly IBarcodeScannerService? _scanner;

    public IBarcodeScannerService? Scanner => _scanner;
    public bool CanScanQr => _scanner is not null;

    public event Action<bool>? CameraReadyChanged;

    [ObservableProperty]
    private bool _isScanningQr;

    public PairingViewModel(IPosSyncClientService syncClient, IBarcodeScannerService? scanner = null)
    {
        _syncClient = syncClient;
        _scanner = scanner;
        _syncClient.StateChanged += HandleSyncStateChanged;
        _syncClient.Kicked += HandleKicked;

        if (_scanner is not null)
        {
            _scanner.BarcodeScanned += HandlePairingScan;
        }
    }

    [RelayCommand]
    private async Task StartQrScan()
    {
        if (_scanner is null) return;

        ErrorMessage = null;
        CameraPermission = await _scanner.RequestCameraPermissionAsync();

        if (CameraPermission != CameraPermissionStatus.Granted)
        {
            ErrorMessage = "Camera permission is required to scan a pairing QR code.";
            return;
        }

        IsScanningQr = true;
        CameraReadyChanged?.Invoke(true);
    }

    [RelayCommand]
    private async Task CancelQrScan() => await StopQrScanInternalAsync();

    private async Task StopQrScanInternalAsync()
    {
        if (!IsScanningQr) return;

        IsScanningQr = false;
        CameraReadyChanged?.Invoke(false);

        if (_scanner is not null)
        {
            await _scanner.StopAsync();
        }
    }

    // Payload is plain "host:port", matching what the desktop QR encodes.
    private void HandlePairingScan(string payload)
    {
        if (!IsScanningQr) return;

        // Split on the LAST colon so nothing breaks if a future payload ever
        // used a host containing one (defensive, not expected today).
        var separatorIndex = payload.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == payload.Length - 1 ||
            !int.TryParse(payload[(separatorIndex + 1)..], out var port))
        {
            ErrorMessage = "That QR code isn't a valid pairing code.";
            return;
        }

        HostInput = payload[..separatorIndex];
        PortInput = port.ToString();

        _ = StopQrScanInternalAsync();

        if (ConnectCommand.CanExecute(null))
        {
            ConnectCommand.Execute(null);
        }
    }

    public void ReportQrScanError(string message) => ErrorMessage = message;

    // Called by MobileShellViewModel when navigating away from this tab.
    public Task DeactivateCameraAsync() => StopQrScanInternalAsync();
}