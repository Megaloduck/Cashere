using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
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

    private async void OnPickProductPhotoClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductAdminViewModel vm || !vm.CanManage) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a product photo",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" } }
            }
        });

        var file = files.FirstOrDefault();
        if (file is null) return;

        await using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        await vm.SetFormPhotoAsync(memory.ToArray(), file.Name);
    }
}
