using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Cashere.ViewModels.Admin;

// Sole owner of ServerBindAddress/ServerPort - previously lived on
// ReceiptAdminViewModel, split out into its own Configuration menu item.
// Holds the loaded ReceiptAdmin entity so Save() can round-trip the
// shop-profile fields (ShopName, Address, etc.) this screen doesn't edit,
// the mirror image of what ReceiptAdminViewModel now does for these two.
public partial class SyncronizationAdminViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;
    private readonly IPairingQrCodeService? _pairingQrCode;

    private ReceiptAdmin? _loadedSettings;
    private string _loadedServerBindAddress = "0.0.0.0";
    private int _loadedServerPort = 5177;

    [ObservableProperty] private string _serverBindAddress = "0.0.0.0";
    [ObservableProperty] private string _serverPort = "5177";
    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private Bitmap? _pairingQrCodeImage;

    public ObservableCollection<string> AvailableBindAddresses { get; } = new();

    // Driven by the image, not the service: an injected service that fails
    // to produce a PNG (bad host, exception) must not leave an empty card
    // on screen. If the bitmap is there, we have something to show.
    public bool IsPairingQrCodeAvailable => PairingQrCodeImage is not null;

    public SyncronizationAdminViewModel(
        IShopContextService shopContext,
        IPairingQrCodeService? pairingQrCode = null)
    {
        _shopContext = shopContext;
        _pairingQrCode = pairingQrCode;
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
            // A QR code is a convenience for pairing, not a hard requirement -
            // an unreachable/failing generator must not take down the screen.
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

    // Same best-effort LAN-adapter discovery ReceiptAdminViewModel used to
    // do - moved here since bind address is now this screen's concern.
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
            // Detection is a convenience, not a requirement - "0.0.0.0"
            // above is always enough to keep the server working.
        }

        if (!string.IsNullOrWhiteSpace(currentlySaved) && !AvailableBindAddresses.Contains(currentlySaved))
        {
            AvailableBindAddresses.Add(currentlySaved);
        }
    }

    // CommunityToolkit.Mvvm calls this whenever PairingQrCodeImage changes,
    // which is how the XAML's IsVisible binding on the QR panel stays in sync.
    partial void OnPairingQrCodeImageChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(IsPairingQrCodeAvailable));
    }

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

        // _loadedSettings carries ShopName/Address/etc. through untouched -
        // this screen only ever mutates the two network fields on it.
        _loadedSettings.ServerBindAddress = ServerBindAddress.Trim();
        _loadedSettings.ServerPort = port;

        await _shopContext.UpdateSettingsAsync(_loadedSettings);

        var networkChanged = ServerBindAddress.Trim() != _loadedServerBindAddress || port != _loadedServerPort;
        _loadedServerBindAddress = ServerBindAddress.Trim();
        _loadedServerPort = port;

        StatusMessage = networkChanged
            ? "Saved. Restart Cashere for the new address/port to take effect."
            : "Saved.";
    }
}