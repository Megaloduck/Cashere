using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Cashere.Android.Services;
using Cashere.Services;

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
    private CameraXBarcodeScannerService? _scannerService;
    private CameraXPhotoCaptureService? _photoCaptureService;

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Wired here (mirrors Cashere.Desktop/Program.cs) so all services
        // are set before App.OnFrameworkInitializationCompleted builds the
        // mobile shell.
        var syncClient = new SignalRPosSyncClientService();
        AppServices.SyncClient = syncClient;
        AppServices.ProductPhoto = new HttpProductPhotoService(syncClient);

        _scannerService = new CameraXBarcodeScannerService(this);
        AppServices.BarcodeScanner = _scannerService;

        _photoCaptureService = new CameraXPhotoCaptureService(this);
        AppServices.PhotoCapture = _photoCaptureService;

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        _scannerService?.CompletePermissionRequest(requestCode, grantResults);
        _photoCaptureService?.CompletePermissionRequest(requestCode, grantResults);
    }
}