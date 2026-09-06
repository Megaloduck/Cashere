using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Cashere.ViewModels.Mobile;

namespace Cashere.Views.Mobile;

public partial class PairingView : UserControl
{
    private Control? _cameraPreviewControl;

    public PairingView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PairingViewModel vm)
        {
            vm.CameraReadyChanged += OnCameraReadyChanged;
        }
    }

    private void OnCameraReadyChanged(bool ready)
    {
        if (DataContext is not PairingViewModel vm || vm.Scanner is null) return;

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