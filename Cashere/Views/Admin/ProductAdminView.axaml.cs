using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Models;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class ProductAdminView : UserControl
{
    public ProductAdminView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Product product } && DataContext is ProductAdminViewModel vm)
        {
            vm.EditProductCommand.Execute(product);
        }
    }

    private void OnToggleActiveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Product product } && DataContext is ProductAdminViewModel vm)
        {
            vm.ToggleActiveCommand.Execute(product);
        }
    }

    private void OnRemovePhotoClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Product product } && DataContext is ProductAdminViewModel vm)
        {
            vm.RemovePhotoCommand.Execute(product);
        }
    }
}