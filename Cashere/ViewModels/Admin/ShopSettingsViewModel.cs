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

    // Captured on load so Save() can round-trip ServerBindAddress/ServerPort -
    // now owned by SyncronizationAdminViewModel - without clobbering whatever
    // was last saved there.
    private ShopSettings? _loadedSettings;

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _currency = "IDR";
    [ObservableProperty] private string _taxRatePercent = "0";
    [ObservableProperty] private string _receiptFooterText = string.Empty;
    [ObservableProperty] private string? _statusMessage;

    public ShopSettingsViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        _loadedSettings = settings;

        ShopName = settings.ShopName;
        Address = settings.Address ?? string.Empty;
        Phone = settings.Phone ?? string.Empty;
        Currency = settings.Currency;
        TaxRatePercent = settings.TaxRatePercent.ToString();
        ReceiptFooterText = settings.ReceiptFooterText ?? string.Empty;
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

        // _loadedSettings carries ServerBindAddress/ServerPort through
        // untouched - this screen doesn't own those fields anymore.
        var settings = _loadedSettings ?? new ShopSettings();
        settings.ShopName = ShopName.Trim();
        settings.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
        settings.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        settings.Currency = Currency.Trim();
        settings.TaxRatePercent = taxRate;
        settings.ReceiptFooterText = string.IsNullOrWhiteSpace(ReceiptFooterText) ? null : ReceiptFooterText.Trim();

        await _shopContext.UpdateSettingsAsync(settings);
        _loadedSettings = settings;

        StatusMessage = "Saved.";
    }
}