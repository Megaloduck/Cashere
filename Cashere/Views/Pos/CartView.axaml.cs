using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.ViewModels.Pos;

namespace Cashere.Views.Pos;

public partial class CartView : UserControl
{
    public CartView()
    {
        InitializeComponent();
    }

    private void OnRemoveLineClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CartLineViewModel line } && DataContext is CartViewModel vm)
        {
            vm.RemoveLineCommand.Execute(line);
        }
    }
}
