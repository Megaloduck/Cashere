using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml; 
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using Cashere.Services;
using Cashere.ViewModels.Admin.Settings;
using System;

namespace Cashere.Views.Admin.Settings;

public partial class DataBackupSettingsView : UserControl
{
    public DataBackupSettingsView()
    {
        InitializeComponent();
    }

    private void OnRestoreClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BackupFileInfo backup } && DataContext is DataBackupSettingsViewModel vm)
        {
            vm.RestoreCommand.Execute(backup);
        }
    }

    private void OnDeleteBackupClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BackupFileInfo backup } && DataContext is DataBackupSettingsViewModel vm)
        {
            vm.DeleteBackupCommand.Execute(backup);
        }
    }

    private async void OnImportHistoricalSalesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataBackupSettingsViewModel vm || !vm.CanImportHistoricalSales || vm.IsImportingSales) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import historical sales workbook",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Excel workbook") { Patterns = new[] { "*.xlsx" } } }
        });
        var file = files.FirstOrDefault();
        if (file is null) return;
        await using var stream = await file.OpenReadAsync();
        if (stream.Length > 25 * 1024 * 1024)
        {
            vm.ImportStatusMessage = "Choose an XLSX file smaller than 25 MB.";
            return;
        }
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        await vm.ImportHistoricalSalesAsync(memory.ToArray());
    }

    private async void OnCreateSalesImportTemplateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataBackupSettingsViewModel vm || !vm.CanImportHistoricalSales) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;
        var bytes = await vm.CreateSalesImportTemplateAsync();
        if (bytes is null) return;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save historical sales import template",
            SuggestedFileName = $"cashere-sales-import-template-{DateTime.Now:yyyyMMdd}.xlsx",
            FileTypeChoices = new[] { new FilePickerFileType("Excel workbook") { Patterns = new[] { "*.xlsx" } } }
        });
        if (file is null) return;
        await using var output = await file.OpenWriteAsync();
        await output.WriteAsync(bytes);
        vm.ImportStatusMessage = $"Template saved: {file.Name}. Fill its sheets and import the workbook when ready.";
    }
}
