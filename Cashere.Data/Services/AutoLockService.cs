using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using Avalonia.Threading;

namespace Cashere.Services;

// App-wide idle timer backing Settings -> Security's "lock after
// inactivity". A static service (same shape as ThemeApplier) rather than
// something injected, since MainWindow's code-behind needs to poke it on
// every pointer/key event without knowing anything about the current
// ViewModel graph. Built on DispatcherTimer specifically so Tick fires back
// on the UI thread - subscribers (RootViewModel) can touch CurrentView
// directly with no manual marshalling.
public static class AutoLockService
{
    private static DispatcherTimer? _timer;
    private static bool _enabled;
    private static int _timeoutMinutes = 15;

    public static event Action? LockTriggered;

    // Called from SecuritySettingsViewModel.Save() for an immediate effect,
    // and from RootViewModel right after a successful login to arm it with
    // whatever's currently saved.
    public static void Configure(bool enabled, int timeoutMinutes)
    {
        _enabled = enabled;
        _timeoutMinutes = timeoutMinutes > 0 ? timeoutMinutes : 15;
        Restart();
    }

    // Called from MainWindow on every pointer/key event - a no-op whenever
    // locking is off, so this costs nothing on installs that don't use it.
    public static void NotifyActivity()
    {
        if (_enabled)
        {
            Restart();
        }
    }

    // Called when returning to the login screen (logout, or the lock itself
    // firing) so a signed-out app never silently re-locks itself again.
    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    private static void Restart()
    {
        _timer?.Stop();
        _timer = null;

        if (!_enabled) return;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(_timeoutMinutes)
        };
        _timer.Tick += (_, _) =>
        {
            Stop();
            LockTriggered?.Invoke();
        };
        _timer.Start();
    }
}