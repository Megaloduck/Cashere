using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Cashere.Services;

public enum CameraPermissionStatus
{
    Unknown,
    Granted,
    Denied
}

// Implemented in Cashere.Android using CameraX + ZXing.Net. The shared
// project only knows about this contract - keeps it free of any
// Android/camera-specific dependency, same as IPosSyncClientService keeps
// this project free of the SignalR client.
public interface IBarcodeScannerService
{
    CameraPermissionStatus PermissionStatus { get; }

    // Raised whenever a barcode is decoded from the live preview. Always
    // raised on the UI thread so subscribers can update bound properties
    // directly.
    event Action<string>? BarcodeScanned;

    Task<CameraPermissionStatus> RequestCameraPermissionAsync();

    // Returns the native camera preview surface ready to drop into an
    // Avalonia layout. Attach it to the visual tree before calling
    // StartAsync().
    Control CreatePreviewControl();

    Task StartAsync();
    Task StopAsync();
}