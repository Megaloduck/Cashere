using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Cashere.Android.Services;
using Cashere.Services;

namespace Cashere.Android;

[Activity(
    Label = "Cashere.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Wired here (mirrors Cashere.Desktop/Program.cs) so it's set before
        // App.OnFrameworkInitializationCompleted builds the pairing screen.
        AppServices.SyncClient = new SignalRPosSyncClientService();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}