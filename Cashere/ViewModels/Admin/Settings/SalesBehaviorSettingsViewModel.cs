using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

public partial class SalesBehaviorSettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    [ObservableProperty] private bool _requireCustomerBeforeCheckout;
    [ObservableProperty] private bool _autoPrintReceiptAfterPayment;
    [ObservableProperty] private bool _enableHeldOrders = true;
    [ObservableProperty] private string _saleNumberPrefix = "S";
    [ObservableProperty] private bool _includeDateInSaleNumber = true;
    [ObservableProperty] private string _saleNumberSequenceDigits = "4";
    [ObservableProperty] private string _orderTypes = "Sale";
    [ObservableProperty] private string? _statusMessage;

    public SalesBehaviorSettingsViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        RequireCustomerBeforeCheckout = settings.RequireCustomerBeforeCheckout;
        AutoPrintReceiptAfterPayment = settings.AutoPrintReceiptAfterPayment;
        EnableHeldOrders = settings.EnableHeldOrders;
        SaleNumberPrefix = settings.SaleNumberPrefix;
        IncludeDateInSaleNumber = settings.IncludeDateInSaleNumber;
        SaleNumberSequenceDigits = settings.SaleNumberSequenceDigits.ToString();
        OrderTypes = settings.OrderTypes;
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var prefix = SaleNumberPrefix.Trim();
        if (prefix.Length is < 1 or > 10 || prefix.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
        {
            StatusMessage = "Sale number prefix must be 1-10 characters using letters, numbers, hyphens or underscores.";
            return;
        }

        if (!int.TryParse(SaleNumberSequenceDigits, out var sequenceDigits) || sequenceDigits is < 1 or > 9)
        {
            StatusMessage = "Sale number sequence length must be between 1 and 9 digits.";
            return;
        }

        var orderTypes = OrderTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (orderTypes.Count is < 1 or > 10 || orderTypes.Any(type => type.Length > 32 || type.Any(char.IsControl)))
        {
            StatusMessage = "Enter 1–10 order types, each 1–32 characters, separated by commas.";
            return;
        }

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.RequireCustomerBeforeCheckout = RequireCustomerBeforeCheckout;
        settings.AutoPrintReceiptAfterPayment = AutoPrintReceiptAfterPayment;
        settings.EnableHeldOrders = EnableHeldOrders;
        settings.SaleNumberPrefix = prefix;
        settings.IncludeDateInSaleNumber = IncludeDateInSaleNumber;
        settings.SaleNumberSequenceDigits = sequenceDigits;
        settings.OrderTypes = string.Join(", ", orderTypes);

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}
