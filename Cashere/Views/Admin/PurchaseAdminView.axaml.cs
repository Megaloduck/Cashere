using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class PurchaseAdminView : UserControl
{
    public PurchaseAdminView()
    {
        InitializeComponent();
    }

    private void OnRemoveFormLineClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PurchaseLineViewModel line } && DataContext is PurchaseAdminViewModel vm)
        {
            vm.RemoveLineCommand.Execute(line);
        }
    }
}