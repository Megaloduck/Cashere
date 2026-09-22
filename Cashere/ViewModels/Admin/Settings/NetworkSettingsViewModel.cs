using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Cashere.ViewModels.Admin.Settings;

// Single ViewModel for the whole Network settings screen: local-server
// bind address/port + pairing QR code (formerly SyncronizationAdminViewModel)
// and the phones currently connected to it (formerly DevicesAdminViewModel).
// Merged into one class since both halves back a single screen and were
// never independently navigable - keeping them separate let the pairing QR
// service get wired to an orphaned, never-rendered instance while the
// actually-displayed one silently missed it.
public partial class NetworkSettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;
    private readonly IPairingQrCodeService? _pairingQrCode;
    private readonly IConnectedDeviceService? _connectedDevices;

    private ReceiptAdmin? _loadedSettings;
    private string _loadedServerBindAddress = "0.0.0.0";
    private int _loadedServerPort = 5177;

    // ---- Synchronization: bind address / port / pairing QR ----

    [ObservableProperty] private string _serverBindAddress = "0.0.0.0";
    [ObservableProperty] private string _serverPort = "5177";
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private Bitmap? _pairingQrCodeImage;

    public ObservableCollection<string> AvailableBindAddresses { get; } = new();

    // Driven by the image, not the service: an injected service that fails
    // to produce a PNG (bad host, exception) must not leave an empty card
    // on screen. If the bitmap is there, we have something to show.
    public bool IsPairingQrCodeAvailable => PairingQrCodeImage is not null;

    // ---- Connected devices ----

    public ObservableCollection<ConnectedDeviceInfo> Devices { get; } = new();

    [ObservableProperty]
    private bool _isServiceAvailable;

    public bool HasNoDevices => IsServiceAvailable && Devices.Count == 0;

    public NetworkSettingsViewModel(
        IShopContextService shopContext,
        IConnectedDeviceService? connectedDevices,
        IPairingQrCodeService? pairingQrCode = null)
    {
        _shopContext = shopContext;
        _connectedDevices = connectedDevices;
        _pairingQrCode = pairingQrCode;

        IsServiceAvailable = connectedDevices is not null;

        if (_connectedDevices is not null)
        {
            // Raised from SignalR's own threads (not the UI thread) whenever
            // a phone connects/disconnects - marshal back onto the UI thread
            // before touching the bound ObservableCollection.
            _connectedDevices.DevicesChanged += () => Dispatcher.UIThread.Post(RefreshDevices);
        }
    }

    public async Task InitializeAsync()
    {
        await LoadAsync();
        RefreshDevices();
    }

    public async Task RefreshAsync()
    {
        await LoadAsync();
        RefreshDevices();
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        _loadedSettings = settings;
        _loadedServerBindAddress = settings.ServerBindAddress;
        _loadedServerPort = settings.ServerPort;

        RegeneratePairingQrCode();

        ServerBindAddress = settings.ServerBindAddress;
        ServerPort = settings.ServerPort.ToString();

        PopulateAvailableBindAddresses(settings.ServerBindAddress);
    }

    // Generated from the currently ACTIVE settings, not the live-edited
    // ServerBindAddress/ServerPort fields - a bind address/port change only
    // takes effect after restart, so the QR must reflect what's actually
    // listening right now, never an unsaved edit that wouldn't work yet.
    private void RegeneratePairingQrCode()
    {
        if (_pairingQrCode is null)
        {
            PairingQrCodeImage = null;
            return;
        }

        try
        {
            var host = ResolveConnectableAddress(_loadedServerBindAddress);
            var png = _pairingQrCode.GeneratePairingQrCodePng(host, _loadedServerPort);

            if (png is null || png.Length == 0)
            {
                PairingQrCodeImage = null;
                return;
            }

            using var stream = new MemoryStream(png);
            PairingQrCodeImage = new Bitmap(stream);
        }
        catch
        {
            PairingQrCodeImage = null;
        }
    }

    // "0.0.0.0" (bind all adapters) isn't itself reachable - resolve to an
    // actual LAN IPv4 address a phone on the same network can connect to.
    private static string ResolveConnectableAddress(string bindAddress)
    {
        if (!string.IsNullOrWhiteSpace(bindAddress) && bindAddress != "0.0.0.0")
        {
            return bindAddress;
        }

        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                              && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(addr => addr.Address.ToString())
                .FirstOrDefault() ?? "0.0.0.0";
        }
        catch
        {
            return "0.0.0.0";
        }
    }

    private void PopulateAvailableBindAddresses(string currentlySaved)
    {
        AvailableBindAddresses.Clear();
        AvailableBindAddresses.Add("0.0.0.0");

        try
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                              && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(addr => addr.Address.ToString())
                .Distinct()
                .OrderBy(a => a);

            foreach (var address in addresses)
            {
                AvailableBindAddresses.Add(address);
            }
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(currentlySaved) && !AvailableBindAddresses.Contains(currentlySaved))
        {
            AvailableBindAddresses.Add(currentlySaved);
        }
    }

    partial void OnPairingQrCodeImageChanged(Bitmap? value) => OnPropertyChanged(nameof(IsPairingQrCodeAvailable));

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        if (_loadedSettings is null)
        {
            StatusMessage = "Settings haven't finished loading yet - try again in a moment.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ServerBindAddress))
        {
            StatusMessage = "Choose a bind address (0.0.0.0 for all networks).";
            return;
        }

        if (!int.TryParse(ServerPort, out var port) || port < 1 || port > 65535)
        {
            StatusMessage = "Port must be a number between 1 and 65535.";
            return;
        }

        _loadedSettings.ServerBindAddress = ServerBindAddress.Trim();
        _loadedSettings.ServerPort = port;

        await _shopContext.UpdateSettingsAsync(_loadedSettings);

        var networkChanged = ServerBindAddress.Trim() != _loadedServerBindAddress || port != _loadedServerPort;
        _loadedServerBindAddress = ServerBindAddress.Trim();
        _loadedServerPort = port;

        RegeneratePairingQrCode();

        StatusMessage = networkChanged
            ? "Saved. Restart Cashere for the new address/port to take effect."
            : "Saved.";
    }

    // ---- Connected devices ----

    private void RefreshDevices()
    {
        if (_connectedDevices is null) return;

        var latest = _connectedDevices.GetConnectedDevices();

        Devices.Clear();
        foreach (var device in latest)
        {
            Devices.Add(device);
        }

        OnPropertyChanged(nameof(HasNoDevices));
    }

    [RelayCommand]
    private void Refresh() => RefreshDevices();

    [RelayCommand]
    private async Task Kick(ConnectedDeviceInfo? device)
    {
        if (device is null || _connectedDevices is null) return;

        await _connectedDevices.KickDeviceAsync(device.ConnectionId);

        // Optimistic removal for a snappier feel - the server's
        // RegisterDisconnected also fires DevicesChanged once the phone's
        // own disconnect completes, which just re-confirms the same state.
        Devices.Remove(device);
        OnPropertyChanged(nameof(HasNoDevices));
    }
}