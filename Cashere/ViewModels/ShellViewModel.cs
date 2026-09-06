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

// Top-level navigation host for the desktop app. MainWindow's ContentControl
// binds to CurrentView; the globally-registered ViewLocator resolves whichever
// concrete View matches the runtime type assigned here (PosViewModel ->
// Views.Pos.PosView, AdminViewModel -> Views.Admin.AdminView) - exactly the
// mechanism the MainWindow.axaml comment already anticipated.
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
        // Catches any product/price/stock changes made while in admin.
        await Pos.ProductPicker.RefreshProductsAsync();
    }

    [RelayCommand]
    private async Task ShowAdmin()
    {
        CurrentView = Admin;
        await Admin.InitializeAsync();
    }
}