using System;
using System.Collections.Generic;
using System.Threading.Tasks;   
using System.Collections.ObjectModel;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

public partial class PaymentSettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;
    private readonly ITaxRateAdminService? _taxRateAdmin;

    public IReadOnlyList<RoundingMode> RoundingModes { get; } = Enum.GetValues<RoundingMode>();
    public bool IsTaxRateServiceAvailable => _taxRateAdmin is not null;
    public ObservableCollection<TaxRate> TaxRates { get; } = new();
    public ObservableCollection<CategoryTaxAssignmentItem> CategoryAssignments { get; } = new();

    [ObservableProperty] private bool _cashEnabled = true;
    [ObservableProperty] private bool _qrisEnabled = true;
    [ObservableProperty] private bool _edcEnabled = true;
    [ObservableProperty] private string _qrisAccountInfo = string.Empty;
    [ObservableProperty] private string _edcAccountInfo = string.Empty;
    [ObservableProperty] private bool _requireConfirmationForNonCash;
    [ObservableProperty] private string _cashFeePercent = "0";
    [ObservableProperty] private string _qrisFeePercent = "0";
    [ObservableProperty] private string _edcFeePercent = "0";
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string _taxRatePercent = "0";
    [ObservableProperty] private RoundingMode _roundingMode = RoundingMode.None;
    [ObservableProperty] private string _roundingIncrement = "0";
    [ObservableProperty] private bool _pricesIncludeTax;
    [ObservableProperty] private string _newTaxRateName = string.Empty;
    [ObservableProperty] private string _newTaxRateValue = "0";
    [ObservableProperty] private string? _taxRateErrorMessage;

    public bool ShowRoundingIncrement => RoundingMode != RoundingMode.None;

    public PaymentSettingsViewModel(IShopContextService shopContext, ITaxRateAdminService? taxRateAdmin = null)
    {
        _shopContext = shopContext;
        _taxRateAdmin = taxRateAdmin;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is not null)
        {
            CashEnabled = settings.CashEnabled;
            QrisEnabled = settings.QrisEnabled;
            EdcEnabled = settings.EdcEnabled;
            QrisAccountInfo = settings.QrisAccountInfo ?? string.Empty;
            EdcAccountInfo = settings.EdcAccountInfo ?? string.Empty;
            RequireConfirmationForNonCash = settings.RequireConfirmationForNonCash;
            CashFeePercent = settings.CashFeePercent.ToString();
            QrisFeePercent = settings.QrisFeePercent.ToString();
            EdcFeePercent = settings.EdcFeePercent.ToString();
            TaxRatePercent = settings.TaxRatePercent.ToString();
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
        await LoadAsync();
    }

    private async Task SaveCategoryTaxRateAsync(int categoryId, int? taxRateId)
    {
        if (_taxRateAdmin is not null)
            await _taxRateAdmin.AssignCategoryTaxRateAsync(categoryId, taxRateId);
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        if (!CashEnabled && !QrisEnabled && !EdcEnabled)
        {
            StatusMessage = "At least one payment method must stay enabled.";
            return;
        }

        if (!decimal.TryParse(CashFeePercent, out var cashFee) || cashFee < 0 ||
            !decimal.TryParse(QrisFeePercent, out var qrisFee) || qrisFee < 0 ||
            !decimal.TryParse(EdcFeePercent, out var edcFee) || edcFee < 0)
        {
            StatusMessage = "Payment fees must be valid, non-negative percentages.";
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

        settings.CashEnabled = CashEnabled;
        settings.QrisEnabled = QrisEnabled;
        settings.EdcEnabled = EdcEnabled;
        settings.QrisAccountInfo = string.IsNullOrWhiteSpace(QrisAccountInfo) ? null : QrisAccountInfo.Trim();
        settings.EdcAccountInfo = string.IsNullOrWhiteSpace(EdcAccountInfo) ? null : EdcAccountInfo.Trim();
        settings.RequireConfirmationForNonCash = RequireConfirmationForNonCash;
        settings.CashFeePercent = cashFee;
        settings.QrisFeePercent = qrisFee;
        settings.EdcFeePercent = edcFee;
        settings.TaxRatePercent = taxRate;
        settings.RoundingMode = RoundingMode;
        settings.RoundingIncrement = roundingIncrement;
        settings.PricesIncludeTax = PricesIncludeTax;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}
