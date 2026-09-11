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
using Android.Graphics;
using Android.Util;

namespace Cashere.Android.Controls;

// Hosts CameraX's PreviewView (a native Android View) inside the Avalonia
// visual tree via NativeControlHost - this is the standard way to embed a
// platform view Avalonia has no first-party equivalent for.
public class CameraPreviewControl : NativeControlHost
{
    // Matches Styles/BauhausTheme.axaml's RadiusLarge (14) - kept as a literal
    // here on purpose. NativeControlHost content is a native Android View
    // layered on top of the Avalonia surface, so it never receives Avalonia's
    // own ClipToBounds/CornerRadius - the rounded "card" Border hosting this
    // control has zero effect on it. Rounding has to happen on the native
    // View itself via an outline + ClipToOutline, or the live camera feed's
    // square corners visibly poke out past the rounded border around it.
    private const float CornerRadiusDip = 14f;

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

        var radiusPx = TypedValue.ApplyDimension(ComplexUnitType.Dip, CornerRadiusDip, context.Resources?.DisplayMetrics);
        PreviewView.OutlineProvider = new RoundedOutlineProvider(radiusPx);
        PreviewView.ClipToOutline = true;

        return new AndroidViewControlHandle(PreviewView);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        PreviewView = null;
        base.DestroyNativeControlCore(control);
    }

    // Draws a rounded-rect clip mask sized to whatever CameraX lays the
    // PreviewView out at - the platform automatically calls GetOutline again
    // whenever the view's bounds change, so this stays correct across
    // rotation/resizing with no extra wiring needed.
    private sealed class RoundedOutlineProvider : ViewOutlineProvider
    {
        private readonly float _radiusPx;

        public RoundedOutlineProvider(float radiusPx)
        {
            _radiusPx = radiusPx;
        }

        public override void GetOutline(View? view, Outline? outline)
        {
            if (view is null || outline is null) return;
            outline.SetRoundRect(0, 0, view.Width, view.Height, _radiusPx);
        }
    }
}