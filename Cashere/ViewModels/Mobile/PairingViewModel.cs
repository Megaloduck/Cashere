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

    public ObservableCollection<SyncCartLine> CartLines { get; } = new();

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

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool CanConnect => !IsBusy && State != SyncConnectionState.Connected && State != SyncConnectionState.Connecting;

    public PairingViewModel(IPosSyncClientService syncClient)
    {
        _syncClient = syncClient;
        _syncClient.StateChanged += HandleSyncStateChanged;
        _syncClient.CartUpdated += HandleCartUpdated;
    }

    // Pre-fills the last shop this device paired with, so the cashier isn't
    // retyping an IP address every shift.
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
        await _syncClient.DisconnectAsync();
        ShopName = null;
        CartLines.Clear();
        CartSubtotal = 0;
        LastScanMessage = string.Empty;
    }

    // Manual entry stands in for the camera scanner until that phase lands -
    // it exercises the exact same ScanBarcode round trip the camera will use,
    // which is the whole point of doing the wiring before the camera work.
    [RelayCommand]
    private async Task SendManualScan()
    {
        if (string.IsNullOrWhiteSpace(ManualBarcode)) return;

        var barcode = ManualBarcode.Trim();
        ManualBarcode = string.Empty;

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