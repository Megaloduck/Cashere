using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Models;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class CustomerAdminView : UserControl
{
    public CustomerAdminView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Customer customer } && DataContext is CustomerAdminViewModel vm)
        {
            vm.EditCustomerCommand.Execute(customer);
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Customer customer } && DataContext is CustomerAdminViewModel vm)
        {
            vm.DeleteCustomerCommand.Execute(customer);
        }
    }
}