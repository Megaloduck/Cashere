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

public partial class PaymentSettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    [ObservableProperty] private bool _cashEnabled = true;
    [ObservableProperty] private bool _qrisEnabled = true;
    [ObservableProperty] private bool _edcEnabled = true;
    [ObservableProperty] private string _qrisAccountInfo = string.Empty;
    [ObservableProperty] private string _edcAccountInfo = string.Empty;
    [ObservableProperty] private bool _requireConfirmationForNonCash;
    [ObservableProperty] private string? _statusMessage;

    public PaymentSettingsViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        CashEnabled = settings.CashEnabled;
        QrisEnabled = settings.QrisEnabled;
        EdcEnabled = settings.EdcEnabled;
        QrisAccountInfo = settings.QrisAccountInfo ?? string.Empty;
        EdcAccountInfo = settings.EdcAccountInfo ?? string.Empty;
        RequireConfirmationForNonCash = settings.RequireConfirmationForNonCash;
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

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.CashEnabled = CashEnabled;
        settings.QrisEnabled = QrisEnabled;
        settings.EdcEnabled = EdcEnabled;
        settings.QrisAccountInfo = string.IsNullOrWhiteSpace(QrisAccountInfo) ? null : QrisAccountInfo.Trim();
        settings.EdcAccountInfo = string.IsNullOrWhiteSpace(EdcAccountInfo) ? null : EdcAccountInfo.Trim();
        settings.RequireConfirmationForNonCash = RequireConfirmationForNonCash;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}