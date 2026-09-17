using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Models;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class VoucherAdminView : UserControl
{
    public VoucherAdminView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Voucher voucher } && DataContext is VoucherAdminViewModel vm)
        {
            vm.EditVoucherCommand.Execute(voucher);
        }
    }

    private void OnToggleActiveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Voucher voucher } && DataContext is VoucherAdminViewModel vm)
        {
            vm.ToggleActiveCommand.Execute(voucher);
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Voucher voucher } && DataContext is VoucherAdminViewModel vm)
        {
            vm.DeleteVoucherCommand.Execute(voucher);
        }
    }
}