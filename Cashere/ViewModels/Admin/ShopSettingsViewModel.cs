using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.ViewModels.Admin;

public partial class ShopSettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    // Captured on load so Save() can tell whether the network section
    // actually changed, and only then mention the restart requirement -
    // editing the shop name shouldn't scare the cashier with a restart
    // notice that has nothing to do with what they just changed.
    private string _loadedServerBindAddress = "0.0.0.0";
    private int _loadedServerPort = 5177;

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _currency = "IDR";
    [ObservableProperty] private string _taxRatePercent = "0";
    [ObservableProperty] private string _receiptFooterText = string.Empty;
    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private string _serverBindAddress = "0.0.0.0";
    [ObservableProperty] private string _serverPort = "5177";

    public ObservableCollection<string> AvailableBindAddresses { get; } = new();

    public ShopSettingsViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        ShopName = settings.ShopName;
        Address = settings.Address ?? string.Empty;
        Phone = settings.Phone ?? string.Empty;
        Currency = settings.Currency;
        TaxRatePercent = settings.TaxRatePercent.ToString();
        ReceiptFooterText = settings.ReceiptFooterText ?? string.Empty;

        _loadedServerBindAddress = settings.ServerBindAddress;
        _loadedServerPort = settings.ServerPort;
        ServerBindAddress = settings.ServerBindAddress;
        ServerPort = settings.ServerPort.ToString();

        PopulateAvailableBindAddresses(settings.ServerBindAddress);
    }

    // Best-effort discovery of this machine's LAN-facing IPv4 addresses, so
    // the admin can pick "just my Wi-Fi adapter" instead of typing an IP by
    // hand. Always includes "0.0.0.0" (all interfaces) first as the
    // recommended default, and never throws - an unusual network stack
    // should just fall back to that one option.
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

        // A previously-saved address might belong to a network this machine
        // isn't on right now (e.g. saved at a different site) - keep it
        // selectable rather than silently dropping the current selection.
        if (!string.IsNullOrWhiteSpace(currentlySaved) && !AvailableBindAddresses.Contains(currentlySaved))
        {
            AvailableBindAddresses.Add(currentlySaved);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        if (string.IsNullOrWhiteSpace(ShopName))
        {
            StatusMessage = "Shop name is required.";
            return;
        }

        if (!decimal.TryParse(TaxRatePercent, out var taxRate))
        {
            StatusMessage = "Tax rate must be a valid number.";
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

        await _shopContext.UpdateSettingsAsync(new ShopSettings
        {
            ShopName = ShopName.Trim(),
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            Currency = Currency.Trim(),
            TaxRatePercent = taxRate,
            ReceiptFooterText = string.IsNullOrWhiteSpace(ReceiptFooterText) ? null : ReceiptFooterText.Trim(),
            ServerBindAddress = ServerBindAddress.Trim(),
            ServerPort = port
        });

        var networkChanged = ServerBindAddress.Trim() != _loadedServerBindAddress || port != _loadedServerPort;
        _loadedServerBindAddress = ServerBindAddress.Trim();
        _loadedServerPort = port;

        StatusMessage = networkChanged
            ? "Saved. Restart Cashere for the new address/port to take effect."
            : "Saved.";
    }
}