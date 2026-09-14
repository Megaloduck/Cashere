using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml; 
using Avalonia.Interactivity;
using Cashere.Services;
using Cashere.ViewModels.Admin.Settings;

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
}