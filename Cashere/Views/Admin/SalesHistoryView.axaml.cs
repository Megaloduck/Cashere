using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Services;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class SalesHistoryView : UserControl
{
    public SalesHistoryView()
    {
        InitializeComponent();
    }

    private void OnViewClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SaleListItem item } && DataContext is SalesHistoryViewModel vm)
        {
            vm.ViewSaleCommand.Execute(item);
        }
    }
}