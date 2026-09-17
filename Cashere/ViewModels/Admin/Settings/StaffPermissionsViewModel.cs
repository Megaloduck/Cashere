using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// Read-only overview of the role model now that login/PIN auth is real
// (LoginViewModel/RootViewModel) - actual cashier CRUD still lives on the
// Cashiers admin screen (CashierAdminViewModel); this page is purely "what
// does each role mean right now", plus a shortcut over to that screen.
// Deliberately not a permissions *editor* yet - Owner and Manager are
// still treated identically everywhere (RolePermissions.CanManage only
// splits Cashier from everyone else), and only the Products screen is
// role-gated so far - see the COMING SOON note in the view for the rest.
public partial class StaffPermissionsViewModel : ViewModelBase
{
    private readonly ICashierAdminService _cashierAdmin;

    public ObservableCollection<Cashier> Cashiers { get; } = new();

    // Re-raised by SettingsShellViewModel and handled by AdminViewModel to
    // flip SelectedSection to AdminSection.Cashiers - same upward-bubbling
    // pattern as AdminViewModel.BackRequested/LogoutRequested.
    public event Action? ManageCashiersRequested;

    public StaffPermissionsViewModel(ICashierAdminService cashierAdmin)
    {
        _cashierAdmin = cashierAdmin;
    }

    public async Task LoadAsync()
    {
        var cashiers = await _cashierAdmin.GetAllCashiersAsync();
        Cashiers.Clear();
        foreach (var cashier in cashiers) Cashiers.Add(cashier);
    }

    [RelayCommand]
    private void ManageCashiers() => ManageCashiersRequested?.Invoke();
}