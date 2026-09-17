using Avalonia.Controls;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cashere.Services;

namespace Cashere.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Tunnel so it sees every pointer/key event before any child
        // control has a chance to mark it handled - every screen in the
        // app (POS, Admin, Settings) lives under this one Window, so this
        // one hook covers all of it. NotifyActivity() no-ops while locking
        // is disabled in Settings -> Security.
        AddHandler(PointerMovedEvent, (_, _) => AutoLockService.NotifyActivity(), RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, (_, _) => AutoLockService.NotifyActivity(), RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, (_, _) => AutoLockService.NotifyActivity(), RoutingStrategies.Tunnel);
    }
}