using Cashere.Converters;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Cashere.ViewModels.Admin.Settings;

// Owns the store-identity fields that used to be editable on
// ReceiptAdminViewModel (ShopName, Address, Phone, Currency, TaxRatePercent),
// plus Email/TaxId/Timezone, and now also: the shop logo and weekly business
// hours. Tax configuration is managed under Payments settings. Save()
// re-fetches the row fresh rather than trusting a possibly-stale
// _loadedSettings snapshot, since several screens can write to this same
// row in one admin session; that avoids clobbering whatever Receipts or
// Synchronization saved most recently.
//
// Timezone configures the shop-hours rule. Displayed timestamps still follow
// this device's local time; see ClockPreferenceService and PreferencesView.
public partial class BusinessInfoViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    public IReadOnlyList<TimezoneOption> TimezoneOptions { get; } = TimezonePresets.FixedOffsets;
    public IReadOnlyList<RoundingMode> RoundingModes { get; } = Enum.GetValues<RoundingMode>();

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _taxId = string.Empty;
    [ObservableProperty] private string _currency = "IDR";
    [ObservableProperty] private TimezoneOption? _timezone;
    [ObservableProperty] private string? _statusMessage;

    // ---- Logo -------------------------------------------------------------

    [ObservableProperty] private string? _logoPath;
    [ObservableProperty] private Bitmap? _logoPreview;

    // Raised so the View's code-behind (which owns the platform file picker,
    // same split as LabelingView owning the camera) can prompt for an image
    // and hand the bytes back via SetLogoAsync.
    public event Action? PickLogoRequested;

    // ---- Business hours -----------------------------------------------------

    public ObservableCollection<BusinessDayFormItem> BusinessHours { get; } = new();
    [ObservableProperty] private bool _enforceBusinessHoursAtCheckout;

    public BusinessInfoViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is not null)
        {
            ShopName = settings.ShopName;
            Address = settings.Address ?? string.Empty;
            Phone = settings.Phone ?? string.Empty;
            Email = settings.Email ?? string.Empty;
            TaxId = settings.TaxId ?? string.Empty;
            Currency = settings.Currency;
            Timezone = TimezonePresets.FindByLabel(settings.Timezone);

            LogoPath = settings.LogoPath;
            RefreshLogoPreview();

            BusinessHours.Clear();
            foreach (var day in BusinessHoursSerializer.Deserialize(settings.BusinessHoursJson))
            {
                BusinessHours.Add(new BusinessDayFormItem(day));
            }
            EnforceBusinessHoursAtCheckout = settings.EnforceBusinessHoursAtCheckout;

        }
    }

    [RelayCommand]
    private void PickLogo() => PickLogoRequested?.Invoke();

    // Called by the View's code-behind once the user has picked a file.
    public async Task SetLogoAsync(byte[] bytes, string fileName)
    {
        LogoPath = LogoPathToImageConverter.SaveLogo(bytes, fileName);
        RefreshLogoPreview();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        LogoPathToImageConverter.DeleteLogo(LogoPath);
        LogoPath = null;
        LogoPreview?.Dispose();
        LogoPreview = null;
    }

    private void RefreshLogoPreview()
    {
        LogoPreview?.Dispose();
        LogoPreview = string.IsNullOrWhiteSpace(LogoPath)
            ? null
            : SafeLoadBitmap(LogoPathToImageConverter.GetFullPath(LogoPath));
    }

    private static Bitmap? SafeLoadBitmap(string fullPath)
    {
        try
        {
            return System.IO.File.Exists(fullPath) ? new Bitmap(fullPath) : null;
        }
        catch
        {
            return null;
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

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.ShopName = ShopName.Trim();
        settings.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
        settings.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        settings.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
        settings.TaxId = string.IsNullOrWhiteSpace(TaxId) ? null : TaxId.Trim();
        settings.Currency = Currency.Trim();
        settings.Timezone = Timezone?.Label;

        settings.LogoPath = LogoPath;
        settings.BusinessHoursJson = BusinessHoursSerializer.Serialize(BusinessHours.Select(d => d.ToModel()));
        settings.EnforceBusinessHoursAtCheckout = EnforceBusinessHoursAtCheckout;
        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}
