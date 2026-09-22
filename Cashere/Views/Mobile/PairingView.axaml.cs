using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Cashere.ViewModels.Mobile;    

namespace Cashere.Views.Mobile;

public partial class PairingView : UserControl
{
    private Control? _cameraPreviewControl;
    private PairingViewModel? _subscribedViewModel;

    public PairingView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Unsubscribe();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Unsubscribe();
        if (DataContext is PairingViewModel vm)
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

    private async void OnCameraReadyChanged(bool ready)
    {
        if (DataContext is not PairingViewModel vm || vm.Scanner is null) return;

        if (ready)
        {
            _cameraPreviewControl ??= vm.Scanner.CreatePreviewControl();
            QrPreviewHost.Content = _cameraPreviewControl;

            try
            {
                await vm.Scanner.StartAsync();
            }
            catch (Exception ex)
            {
                vm.ReportQrScanError($"Could not start camera: {ex.Message}");
            }
        }
        else
        {
            QrPreviewHost.Content = null;
        }
    }
}