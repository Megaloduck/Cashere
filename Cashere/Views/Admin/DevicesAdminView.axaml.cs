using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Cashere.Services;
using Cashere.ViewModels.Admin;

namespace Cashere.Views.Admin;

public partial class DevicesAdminView : UserControl
{
    public DevicesAdminView()
    {
        InitializeComponent();
    }

    private void OnKickClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ConnectedDeviceInfo device } && DataContext is DevicesAdminViewModel vm)
        {
            vm.KickCommand.Execute(device);
        }
    }
}