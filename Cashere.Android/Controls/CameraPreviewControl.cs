using Android.Views;
using AndroidX.Camera.View;
using Avalonia.Android;
using Avalonia.Android.Platform;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Android.Controls;

// Hosts CameraX's PreviewView (a native Android View) inside the Avalonia
// visual tree via NativeControlHost - this is the standard way to embed a
// platform view Avalonia has no first-party equivalent for.
public class CameraPreviewControl : NativeControlHost
{
    public PreviewView? PreviewView { get; private set; }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var context = global::Android.App.Application.Context;
        PreviewView = new PreviewView(context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        return new AndroidViewControlHandle(PreviewView);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        PreviewView = null;
        base.DestroyNativeControlCore(control);
    }
}