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
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.RequireCustomerBeforeCheckout = RequireCustomerBeforeCheckout;
        settings.AutoPrintReceiptAfterPayment = AutoPrintReceiptAfterPayment;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}