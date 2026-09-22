using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Services;
using Cashere.ViewModels.Admin.Settings;

namespace Cashere.Views.Admin.Settings;

public partial class NetworkSettingsView : UserControl
{
    public NetworkSettingsView()
    {
        InitializeComponent();
    }

    private void OnKickClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ConnectedDeviceInfo device } && DataContext is NetworkSettingsViewModel vm)
        {
            vm.KickCommand.Execute(device);
        }
    }
}