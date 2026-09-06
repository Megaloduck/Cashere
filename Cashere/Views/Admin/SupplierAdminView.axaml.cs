using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Models;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class SupplierAdminView : UserControl
{
    public SupplierAdminView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Supplier supplier } && DataContext is SupplierAdminViewModel vm)
        {
            vm.EditSupplierCommand.Execute(supplier);
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Supplier supplier } && DataContext is SupplierAdminViewModel vm)
        {
            vm.DeleteSupplierCommand.Execute(supplier);
        }
    }
}