using Android.App;
using Android.Content.PM;
using Android.Hardware;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Avalonia.Controls;
using Avalonia.Threading;
using Cashere.Android.Controls;
using Cashere.Services;
using Java.Util.Concurrent;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;`
using ZXing;

namespace Cashere.Android.Services;

public class CameraXBarcodeScannerService : IBarcodeScannerService, ImageAnalysis.IAnalyzer
{
    private const int CameraPermissionRequestCode = 4242;

    private readonly Activity _activity;
    private readonly BarcodeReader _reader = new()
    {
        AutoRotate = true,
        Options = new ZXing.Common.DecodingOptions
        {
            TryHarder = true,
            PossibleFormats = new[]
            {
                BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.UPC_A,
                BarcodeFormat.UPC_E, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39,
                BarcodeFormat.QR_CODE
            }
        }
    };

    private CameraPreviewControl? _previewControl;
    private IExecutorService? _analysisExecutor;
    private ProcessCameraProvider? _cameraProvider;
    private TaskCompletionSource<CameraPermissionStatus>? _permissionTcs;

    private string? _lastCode;
    private DateTime _lastCodeAt = DateTime.MinValue;

    public CameraPermissionStatus PermissionStatus { get; private set; } = CameraPermissionStatus.Unknown;

    public event Action<string>? BarcodeScanned;

    public CameraXBarcodeScannerService(Activity activity)
    {
        _activity = activity;
    }

    public Task<CameraPermissionStatus> RequestCameraPermissionAsync()
    {
        var granted = ContextCompat.CheckSelfPermission(_activity, Android.Manifest.Permission.Camera)
            == Permission.Granted;

        if (granted)
        {
            PermissionStatus = CameraPermissionStatus.Granted;
            return Task.FromResult(PermissionStatus);
        }

        _permissionTcs = new TaskCompletionSource<CameraPermissionStatus>();
        ActivityCompat.RequestPermissions(_activity, new[] { Android.Manifest.Permission.Camera }, CameraPermissionRequestCode);
        return _permissionTcs.Task;
    }

    // Called from MainActivity.OnRequestPermissionsResult.
    public void CompletePermissionRequest(int requestCode, Permission[] grantResults)
    {
        if (requestCode != CameraPermissionRequestCode) return;

        PermissionStatus = grantResults.Length > 0 && grantResults[0] == Permission.Granted
            ? CameraPermissionStatus.Granted
            : CameraPermissionStatus.Denied;

        _permissionTcs?.TrySetResult(PermissionStatus);
    }

    public Control CreatePreviewControl()
    {
        _previewControl = new CameraPreviewControl();
        return _previewControl;
    }

    public async Task StartAsync()
    {
        if (_previewControl?.PreviewView is null)
        {
            throw new InvalidOperationException(
                "CreatePreviewControl() must be attached to the visual tree before StartAsync().");
        }

        _cameraProvider ??= await GetCameraProviderAsync();
        _analysisExecutor ??= Executors.NewSingleThreadExecutor();

        var preview = new Preview.Builder().Build();
        preview.SetSurfaceProvider(_previewControl.PreviewView.SurfaceProvider);

        var analysis = new ImageAnalysis.Builder()
            .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
            .Build();
        analysis.SetAnalyzer(_analysisExecutor, this);

        _cameraProvider.UnbindAll();
        _cameraProvider.BindToLifecycle(
            (ILifecycleOwner)_activity, CameraSelector.DefaultBackCamera, preview, analysis);
    }

    public Task StopAsync()
    {
        _cameraProvider?.UnbindAll();
        return Task.CompletedTask;
    }

    // ImageAnalysis.IAnalyzer - runs on the background executor thread set
    // up in StartAsync, one frame at a time (StrategyKeepOnlyLatest drops
    // frames while we're still busy decoding the previous one).
    public void Analyze(IImageProxy image)
    {
        try
        {
            var plane = image.GetPlanes()?.FirstOrDefault();
            if (plane?.Buffer is null) return;

            var buffer = plane.Buffer;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            var source = new PlanarYUVLuminanceSource(
                bytes, image.Width, image.Height, 0, 0, image.Width, image.Height, false);

            var result = _reader.Decode(source);
            if (result is not null)
            {
                RaiseBarcodeScanned(result.Text);
            }
        }
        catch
        {
            // A failed decode on one frame just means "nothing readable this
            // frame" - another frame is a fraction of a second away.
        }
        finally
        {
            image.Close();
        }
    }

    private void RaiseBarcodeScanned(string code)
    {
        var now = DateTime.UtcNow;
        if (code == _lastCode && (now - _lastCodeAt) < TimeSpan.FromSeconds(2))
        {
            return; // debounce - the same barcode is still sitting in frame
        }

        _lastCode = code;
        _lastCodeAt = now;

        Dispatcher.UIThread.Post(() => BarcodeScanned?.Invoke(code));
    }

    private Task<ProcessCameraProvider> GetCameraProviderAsync()
    {
        var tcs = new TaskCompletionSource<ProcessCameraProvider>();
        var future = ProcessCameraProvider.GetInstance(_activity);
        future.AddListener(
            new JavaRunnable(() => tcs.TrySetResult((ProcessCameraProvider)future.Get()!)),
            ContextCompat.GetMainExecutor(_activity));
        return tcs.Task;
    }
}