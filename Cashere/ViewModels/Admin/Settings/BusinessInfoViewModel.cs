using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cashere.ViewModels.Admin.Settings;

// Owns the store-identity fields that used to be editable on
// ReceiptAdminViewModel (ShopName, Address, Phone, Currency, TaxRatePercent),
// plus the Email/TaxId/Timezone fields - all still backed by the same
// single-row ReceiptAdmin table Receipts and Synchronization already write
// to. Save() re-fetches the row fresh rather than trusting a possibly-stale
// _loadedSettings snapshot, since three different screens can now write to
// this same row in one admin session; that avoids clobbering whatever
// Receipts or Synchronization saved most recently.
//
// Timezone here is purely informational (what timezone the shop is
// physically in, e.g. for reference/printed info) - it does not affect how
// any timestamp is displayed. Display always follows this device's own
// local time; see ClockPreferenceService and PreferencesView's note.
public partial class BusinessInfoViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    public IReadOnlyList<TimezoneOption> TimezoneOptions { get; } = TimezonePresets.FixedOffsets;

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _taxId = string.Empty;
    [ObservableProperty] private string _currency = "IDR";
    [ObservableProperty] private TimezoneOption? _timezone;
    [ObservableProperty] private string _taxRatePercent = "0";
    [ObservableProperty] private string? _statusMessage;

    public BusinessInfoViewModel(IShopContextService shopContext)
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
        Email = settings.Email ?? string.Empty;
        TaxId = settings.TaxId ?? string.Empty;
        Currency = settings.Currency;
        TaxRatePercent = settings.TaxRatePercent.ToString();
        Timezone = TimezonePresets.FindByLabel(settings.Timezone);
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

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.ShopName = ShopName.Trim();
        settings.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
        settings.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        settings.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
        settings.TaxId = string.IsNullOrWhiteSpace(TaxId) ? null : TaxId.Trim();
        settings.Currency = Currency.Trim();
        settings.Timezone = Timezone?.Label;
        settings.TaxRatePercent = taxRate;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}