using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Text;
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

    private async void OnImportCatalogClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductAdminViewModel vm || !vm.CanManage || vm.IsDataTransferBusy) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import product catalog CSV",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } } }
        });
        var file = files.FirstOrDefault();
        if (file is null) return;

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        await vm.ImportProductsCsvAsync(await reader.ReadToEndAsync());
    }

    private async void OnExportCatalogClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductAdminViewModel vm || !vm.CanManage || vm.IsDataTransferBusy) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var csv = await vm.ExportProductsCsvAsync();
        if (csv is null) return;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export product catalog",
            SuggestedFileName = $"cashere-products-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } } }
        });
        if (file is null) return;
        await using var output = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteAsync(csv);
        await writer.FlushAsync();
        vm.DataTransferStatus = $"Catalog exported to {file.Name}.";
    }
}
