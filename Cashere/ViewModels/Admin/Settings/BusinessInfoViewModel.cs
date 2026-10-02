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
// plus Email/TaxId/Timezone, and now also: the shop logo, weekly business
// hours, rounding rules, the "prices include tax" toggle, and the
// TaxRate/Category assignment grid (the "multiple tax rates" feature). Save()
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
    private readonly ITaxRateAdminService? _taxRateAdmin;

    public IReadOnlyList<TimezoneOption> TimezoneOptions { get; } = TimezonePresets.FixedOffsets;
    public IReadOnlyList<RoundingMode> RoundingModes { get; } = Enum.GetValues<RoundingMode>();

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _taxId = string.Empty;
    [ObservableProperty] private string _currency = "IDR";
    [ObservableProperty] private TimezoneOption? _timezone;
    [ObservableProperty] private string _taxRatePercent = "0";
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

    // ---- Rounding & tax-inclusive pricing -----------------------------------

    [ObservableProperty] private RoundingMode _roundingMode = RoundingMode.None;
    [ObservableProperty] private string _roundingIncrement = "0";
    [ObservableProperty] private bool _pricesIncludeTax;

    public bool ShowRoundingIncrement => RoundingMode != RoundingMode.None;

    // ---- Tax rates & category assignment ------------------------------------

    public bool IsTaxRateServiceAvailable => _taxRateAdmin is not null;

    public ObservableCollection<TaxRate> TaxRates { get; } = new();
    public ObservableCollection<CategoryTaxAssignmentItem> CategoryAssignments { get; } = new();

    [ObservableProperty] private string _newTaxRateName = string.Empty;
    [ObservableProperty] private string _newTaxRateValue = "0";
    [ObservableProperty] private string? _taxRateErrorMessage;

    public BusinessInfoViewModel(IShopContextService shopContext, ITaxRateAdminService? taxRateAdmin = null)
    {
        _shopContext = shopContext;
        _taxRateAdmin = taxRateAdmin;
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
            TaxRatePercent = settings.TaxRatePercent.ToString();
            Timezone = TimezonePresets.FindByLabel(settings.Timezone);

            LogoPath = settings.LogoPath;
            RefreshLogoPreview();

            BusinessHours.Clear();
            foreach (var day in BusinessHoursSerializer.Deserialize(settings.BusinessHoursJson))
            {
                BusinessHours.Add(new BusinessDayFormItem(day));
            }
            EnforceBusinessHoursAtCheckout = settings.EnforceBusinessHoursAtCheckout;

            RoundingMode = settings.RoundingMode;
            RoundingIncrement = settings.RoundingIncrement.ToString();
            PricesIncludeTax = settings.PricesIncludeTax;
        }

        if (_taxRateAdmin is not null)
        {
            var rates = await _taxRateAdmin.GetAllTaxRatesAsync();
            TaxRates.Clear();
            foreach (var rate in rates) TaxRates.Add(rate);

            var categories = await _taxRateAdmin.GetCategoriesWithTaxRatesAsync();
            CategoryAssignments.Clear();
            foreach (var category in categories)
            {
                CategoryAssignments.Add(new CategoryTaxAssignmentItem(category, TaxRates, SaveCategoryTaxRateAsync));
            }
        }
    }

    partial void OnRoundingModeChanged(RoundingMode value) => OnPropertyChanged(nameof(ShowRoundingIncrement));

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
    private async Task AddTaxRate()
    {
        if (_taxRateAdmin is null) return;

        TaxRateErrorMessage = null;

        if (string.IsNullOrWhiteSpace(NewTaxRateName))
        {
            TaxRateErrorMessage = "Enter a name for the tax rate.";
            return;
        }

        if (!decimal.TryParse(NewTaxRateValue, out var rateValue) || rateValue < 0)
        {
            TaxRateErrorMessage = "Rate must be zero or a positive number.";
            return;
        }

        try
        {
            var created = await _taxRateAdmin.CreateTaxRateAsync(new TaxRateInput(NewTaxRateName, rateValue));
            TaxRates.Add(created);
            NewTaxRateName = string.Empty;
            NewTaxRateValue = "0";
        }
        catch (AdminValidationException ex)
        {
            TaxRateErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteTaxRate(TaxRate? taxRate)
    {
        if (_taxRateAdmin is null || taxRate is null) return;

        await _taxRateAdmin.DeleteTaxRateAsync(taxRate.Id);
        TaxRates.Remove(taxRate);

        // Any category pointing at the deleted rate now falls back to the
        // shop-wide default - reload assignments so the grid reflects that
        // immediately rather than showing a stale selection.
        await LoadAsync();
    }

    private async Task SaveCategoryTaxRateAsync(int categoryId, int? taxRateId)
    {
        if (_taxRateAdmin is null) return;
        await _taxRateAdmin.AssignCategoryTaxRateAsync(categoryId, taxRateId);
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

        if (!decimal.TryParse(TaxRatePercent, out var taxRate) || taxRate < 0)
        {
            StatusMessage = "Tax rate must be zero or a positive number.";
            return;
        }

        if (!decimal.TryParse(RoundingIncrement, out var roundingIncrement) || roundingIncrement < 0)
        {
            StatusMessage = "Rounding increment must be zero or a positive number.";
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

        settings.LogoPath = LogoPath;
        settings.BusinessHoursJson = BusinessHoursSerializer.Serialize(BusinessHours.Select(d => d.ToModel()));
        settings.EnforceBusinessHoursAtCheckout = EnforceBusinessHoursAtCheckout;
        settings.RoundingMode = RoundingMode;
        settings.RoundingIncrement = roundingIncrement;
        settings.PricesIncludeTax = PricesIncludeTax;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}
