using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Cashere.Services;

// Implemented in Cashere.Android using CameraX's ImageCapture use case -
// separate from IBarcodeScannerService's ImageAnalysis pipeline, since a
// still-photo capture and a continuous barcode-decode loop are different
// CameraX use cases bound independently (never simultaneously - they live on
// different tabs). Shares CameraPermissionStatus with IBarcodeScannerService
// since it's the same underlying camera permission either way.
public interface IPhotoCaptureService
{
    CameraPermissionStatus PermissionStatus { get; }

    Task<CameraPermissionStatus> RequestCameraPermissionAsync();

    // Returns the native camera preview surface ready to drop into an
    // Avalonia layout - same pattern as IBarcodeScannerService.CreatePreviewControl.
    Control CreatePreviewControl();

    Task StartAsync();
    Task StopAsync();

    // Captures a single still frame and returns it as JPEG-encoded bytes.
    Task<byte[]> CapturePhotoAsync();
}