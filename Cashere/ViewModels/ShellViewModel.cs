using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.ViewModels.Admin;
using Cashere.ViewModels.Pos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.ViewModels.Admin;
using Cashere.ViewModels.Pos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    public PosViewModel Pos { get; }
    public AdminViewModel Admin { get; }

    [ObservableProperty]
    private ViewModelBase _currentView;

    // Bubbled up from AdminViewModel.LogoutRequested (its sidebar LOGOUT
    // item) so RootViewModel can tear this shell down and show LoginView
    // again - mirrors how AdminRequested/BackRequested already wire Pos<->Admin.
    public event Action? LogoutRequested;

    public ShellViewModel(PosViewModel pos, AdminViewModel admin)
    {
        Pos = pos;
        Admin = admin;
        _currentView = pos;

        Pos.AdminRequested += () => _ = ShowAdmin();
        Admin.BackRequested += () => _ = ShowPos();
        Admin.LogoutRequested += () => LogoutRequested?.Invoke();
    }

    [RelayCommand]
    private async Task ShowPos()
    {
        CurrentView = Pos;
        // Catches any product/price/stock changes, any payment-method
        // enable/disable, any sales-behavior change, and any new/edited
        // customer made while in admin.
        await Pos.ProductPicker.RefreshProductsAsync();
        await Pos.RefreshPaymentSettingsAsync();
        await Pos.RefreshSalesBehaviorSettingsAsync();
        await Pos.RefreshCustomersAsync();
    }

    [RelayCommand]
    private async Task ShowAdmin()
    {
        CurrentView = Admin;
        await Admin.InitializeAsync();
    }
}