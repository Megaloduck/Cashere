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

    public ShellViewModel(PosViewModel pos, AdminViewModel admin)
    {
        Pos = pos;
        Admin = admin;
        _currentView = pos;

        Pos.AdminRequested += () => _ = ShowAdmin();
        Admin.BackRequested += () => _ = ShowPos();
    }

    [RelayCommand]
    private async Task ShowPos()
    {
        CurrentView = Pos;
        // Catches any product/price/stock changes, and any payment-method
        // enable/disable, made while in admin.
        await Pos.ProductPicker.RefreshProductsAsync();
        await Pos.RefreshPaymentSettingsAsync();
    }

    [RelayCommand]
    private async Task ShowAdmin()
    {
        CurrentView = Admin;
        await Admin.InitializeAsync();
    }
}