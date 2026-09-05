using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Models;
using Cashere.ViewModels.Pos;

namespace Cashere.Views.Pos;

public partial class ProductPickerView : UserControl
{
    public ProductPickerView()
    {
        InitializeComponent();
    }

    private void OnProductClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Product product } && DataContext is ProductPickerViewModel vm)
        {
            vm.SelectProductCommand.Execute(product);
        }
    }

    private void OnCategoryClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Category category } && DataContext is ProductPickerViewModel vm)
        {
            vm.SelectedCategory = vm.SelectedCategory?.Id == category.Id ? null : category;
        }
    }

    private void OnAllCategoriesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductPickerViewModel vm)
        {
            vm.SelectedCategory = null;
        }
    }
}
