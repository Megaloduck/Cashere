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

// Connection-only screen now - scanning (camera + manual entry + live cart)
// moved to ScanningViewModel/ScanningView once the mobile UI grew a sidebar
// with dedicated Pairing/Scanning/Labeling sections. Shares the same
// IPosSyncClientService instance as ScanningViewModel, so connecting here is
// immediately reflected there.
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
    private SyncConnectionState _state = SyncConnectionState.Disconnected;

    [ObservableProperty]
    private string? _shopName;

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool CanConnect => !IsBusy && State != SyncConnectionState.Connected && State != SyncConnectionState.Connecting;

    public PairingViewModel(IPosSyncClientService syncClient)
    {
        _syncClient = syncClient;
        _syncClient.StateChanged += HandleSyncStateChanged;
    }

    public async Task InitializeAsync()
    {
        var last = await _syncClient.LoadLastEndpointAsync();
        if (last is not null)
        {
            HostInput = last.Host;
            PortInput = last.Port.ToString();
        }

        // Reflect current state in case the client is already connected.
        State = _syncClient.State;
        ShopName = _syncClient.ShopName;
    }

    private void HandleSyncStateChanged(SyncConnectionState state) => State = state;

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
}