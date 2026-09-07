using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cashere.Models;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class CashierAdminView : UserControl
{
    public CashierAdminView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Cashier cashier } && DataContext is CashierAdminViewModel vm)
        {
            vm.EditCashierCommand.Execute(cashier);
        }
    }

    private void OnToggleActiveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Cashier cashier } && DataContext is CashierAdminViewModel vm)
        {
            vm.ToggleActiveCommand.Execute(cashier);
        }
    }
}