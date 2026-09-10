using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Avalonia.Controls;
using Cashere.Android.Controls;
using Cashere.Services;
using Java.Util.Concurrent;
using CameraPreview = AndroidX.Camera.Core.Preview;

namespace Cashere.Android.Services;

public class CameraXPhotoCaptureService : IPhotoCaptureService
{
    // Different request code from CameraXBarcodeScannerService's 4242, so a
    // permission result can never be misrouted if both were ever mid-flight.
    private const int CameraPermissionRequestCode = 4243;

    private readonly Activity _activity;

    // Serializes StartAsync() calls - LabelingView can raise CameraReadyChanged
    // in quick succession (permission granted, product picked, etc.), and
    // without this a second call could UnbindAll()/BindToLifecycle() while
    // the first is still mid-poll below.
    private readonly SemaphoreSlim _startLock = new(1, 1);

    private CameraPreviewControl? _previewControl;
    private IExecutorService? _captureExecutor;
    private ProcessCameraProvider? _cameraProvider;
    private ImageCapture? _imageCapture;
    private TaskCompletionSource<CameraPermissionStatus>? _permissionTcs;

    public CameraPermissionStatus PermissionStatus { get; private set; } = CameraPermissionStatus.Unknown;

    public CameraXPhotoCaptureService(Activity activity)
    {
        _activity = activity;
    }

    public Task<CameraPermissionStatus> RequestCameraPermissionAsync()
    {
        var granted = ContextCompat.CheckSelfPermission(_activity, global::Android.Manifest.Permission.Camera)
            == Permission.Granted;

        if (granted)
        {
            PermissionStatus = CameraPermissionStatus.Granted;
            return Task.FromResult(PermissionStatus);
        }

        _permissionTcs = new TaskCompletionSource<CameraPermissionStatus>();
        ActivityCompat.RequestPermissions(_activity, new[] { global::Android.Manifest.Permission.Camera }, CameraPermissionRequestCode);
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
        if (_previewControl is null)
        {
            throw new InvalidOperationException(
                "CreatePreviewControl() must be attached to the visual tree before StartAsync().");
        }

        await _startLock.WaitAsync();
        try
        {
            // Avalonia's NativeControlHost (CameraPreviewControl) creates the
            // native Android PreviewView asynchronously, once the control
            // returned by CreatePreviewControl() is actually attached to the
            // visual tree (e.g. assigned to a ContentControl.Content) and a
            // layout pass runs. That can still be pending on the very first
            // frame after assigning Content - previously this made StartAsync
            // throw immediately, and since callers didn't await it, it failed
            // silently, leaving _imageCapture null and the camera never bound.
            // Poll briefly instead of failing on the first check.
            var waited = TimeSpan.Zero;
            var pollInterval = TimeSpan.FromMilliseconds(25);
            var timeout = TimeSpan.FromSeconds(3);
            while (_previewControl.PreviewView is null && waited < timeout)
            {
                await Task.Delay(pollInterval);
                waited += pollInterval;
            }

            if (_previewControl.PreviewView is null)
            {
                throw new InvalidOperationException(
                    "Camera preview surface never became ready - try again.");
            }

            _cameraProvider ??= await GetCameraProviderAsync();
            _captureExecutor ??= Executors.NewSingleThreadExecutor();

            var preview = new CameraPreview.Builder().Build();
            preview.SetSurfaceProvider(
                ContextCompat.GetMainExecutor(_activity),
                _previewControl.PreviewView.SurfaceProvider);

            _imageCapture = new ImageCapture.Builder()
                .SetCaptureMode(ImageCapture.CaptureModeMinimizeLatency)
                .Build();

            _cameraProvider.UnbindAll();
            _cameraProvider.BindToLifecycle(
                (ILifecycleOwner)_activity, CameraSelector.DefaultBackCamera, preview, _imageCapture);
        }
        finally
        {
            _startLock.Release();
        }
    }

    public Task StopAsync()
    {
        _cameraProvider?.UnbindAll();
        _imageCapture = null;
        return Task.CompletedTask;
    }

    public Task<byte[]> CapturePhotoAsync()
    {
        if (_imageCapture is null || _captureExecutor is null)
        {
            throw new InvalidOperationException("StartAsync() must succeed before CapturePhotoAsync() can be called.");
        }

        var tcs = new TaskCompletionSource<byte[]>();
        _imageCapture.TakePicture(_captureExecutor, new CaptureCallback(tcs));
        return tcs.Task;
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

    private sealed class CaptureCallback : ImageCapture.OnImageCapturedCallback
    {
        private readonly TaskCompletionSource<byte[]> _tcs;

        public CaptureCallback(TaskCompletionSource<byte[]> tcs)
        {
            _tcs = tcs;
        }

        public override void OnCaptureSuccess(IImageProxy image)
        {
            try
            {
                var plane = image.GetPlanes()?.FirstOrDefault();
                if (plane?.Buffer is null)
                {
                    _tcs.TrySetException(new InvalidOperationException("Captured image had no data."));
                    return;
                }

                var buffer = plane.Buffer;
                var bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);
                _tcs.TrySetResult(bytes);
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }
            finally
            {
                image.Close();
            }
        }

        public override void OnError(ImageCaptureException exception)
        {
            _tcs.TrySetException(new InvalidOperationException(exception.Message));
        }
    }
}