using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Interactivity;
using Cashere.Services;
using Cashere.ViewModels.Mobile;

namespace Cashere.Views.Mobile;

public partial class LabelingView : UserControl
{
    private Control? _cameraPreviewControl;
    private LabelingViewModel? _subscribedViewModel;

    public LabelingView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Unsubscribe();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Unsubscribe();

        if (DataContext is LabelingViewModel vm)
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
        if (DataContext is not LabelingViewModel vm || vm.PhotoCapture is null) return;

        if (ready)
        {
            _cameraPreviewControl ??= vm.PhotoCapture.CreatePreviewControl();
            CameraPreviewHost.Content = _cameraPreviewControl;
            _ = vm.PhotoCapture.StartAsync();
        }
        else
        {
            CameraPreviewHost.Content = null;
        }
    }

    private void OnProductClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ProductLookupItem item } && DataContext is LabelingViewModel vm)
        {
            vm.SelectProductCommand.Execute(item);
        }
    }
}