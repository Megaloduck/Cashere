using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Cashere.ViewModels.Mobile;

namespace Cashere.Views.Mobile;

public partial class ScanningView : UserControl
{
    private Control? _cameraPreviewControl;
    private ScanningViewModel? _subscribedViewModel;

    public ScanningView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        // The sidebar recreates this View each time you navigate away and
        // back, unlike the old single-screen mobile app - unsubscribe on
        // teardown so a stale View isn't kept alive by the ViewModel's event.
        Unloaded += (_, _) => Unsubscribe();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Unsubscribe();

        if (DataContext is ScanningViewModel vm)
        {
            _subscribedViewModel = vm;
            vm.CameraReadyChanged += OnCameraReadyChanged;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.CameraReadyChanged -= OnCameraReadyChanged;
            _subscribedViewModel = null;
        }
    }

    private void OnCameraReadyChanged(bool ready)
    {
        if (DataContext is not ScanningViewModel vm || vm.Scanner is null) return;

        if (ready)
        {
            _cameraPreviewControl ??= vm.Scanner.CreatePreviewControl();
            CameraPreviewHost.Content = _cameraPreviewControl;
            _ = vm.Scanner.StartAsync();
        }
        else
        {
            CameraPreviewHost.Content = null;
        }
    }
}