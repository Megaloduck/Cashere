using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// Owner-only overview and shortcut for cashier accounts. Operational
// section access is enforced by AdminViewModel; fine-grained permissions
// for POS actions are being added as those actions get their own controls.
public partial class StaffPermissionsViewModel : ViewModelBase
{
    private readonly ICashierAdminService _cashierAdmin;
    private readonly IShopContextService _shopContext;
    public bool CanManageCashierAccounts { get; }

    [ObservableProperty] private bool _cashierCanViewOwnSalesHistory;
    [ObservableProperty] private bool _cashierCanRequestRefunds;
    [ObservableProperty] private bool _cashierCanRequestVoids;
    [ObservableProperty] private bool _cashierCanApplyVouchers = true;
    [ObservableProperty] private string? _statusMessage;

    public ObservableCollection<Cashier> Cashiers { get; } = new();

    // Re-raised by SettingsShellViewModel and handled by AdminViewModel to
    // flip SelectedSection to AdminSection.Cashiers - same upward-bubbling
    // pattern as AdminViewModel.BackRequested/LogoutRequested.
    public event Action? ManageCashiersRequested;

    public StaffPermissionsViewModel(ICashierAdminService cashierAdmin, IShopContextService shopContext, UserRole currentRole)
    {
        _cashierAdmin = cashierAdmin;
        _shopContext = shopContext;
        CanManageCashierAccounts = currentRole == UserRole.Owner;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        CashierCanViewOwnSalesHistory = settings?.CashierCanViewOwnSalesHistory ?? false;
        CashierCanRequestRefunds = settings?.CashierCanRequestRefunds ?? false;
        CashierCanRequestVoids = settings?.CashierCanRequestVoids ?? false;
        CashierCanApplyVouchers = settings?.CashierCanApplyVouchers ?? true;

        var cashiers = await _cashierAdmin.GetAllCashiersAsync();
        Cashiers.Clear();
        foreach (var cashier in cashiers) Cashiers.Add(cashier);
    }

    [RelayCommand]
    private async Task SavePermissions()
    {
        if (!CanManageCashierAccounts) return;
        StatusMessage = null;
        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();
        settings.CashierCanViewOwnSalesHistory = CashierCanViewOwnSalesHistory;
        settings.CashierCanRequestRefunds = CashierCanRequestRefunds;
        settings.CashierCanRequestVoids = CashierCanRequestVoids;
        settings.CashierCanApplyVouchers = CashierCanApplyVouchers;
        await _shopContext.UpdateSettingsAsync(settings);
        StatusMessage = "Cashier permissions saved.";
    }

    [RelayCommand]
    private void ManageCashiers()
    {
        if (CanManageCashierAccounts) ManageCashiersRequested?.Invoke();
    }
}
