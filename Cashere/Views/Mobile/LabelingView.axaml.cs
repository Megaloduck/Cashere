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

        private async void OnCameraReadyChanged(bool ready)
        {
            if (DataContext is not LabelingViewModel vm || vm.PhotoCapture is null) return;

            if (ready)
            {
                // Not actually ready to take a photo until StartAsync below
                // confirms the camera is bound - keeps TAKE PHOTO disabled for
                // the brief window while the native preview surface attaches.
                vm.SetCameraStarted(false);

                _cameraPreviewControl ??= vm.PhotoCapture.CreatePreviewControl();
                CameraPreviewHost.Content = _cameraPreviewControl;

                try
                {
                    await vm.PhotoCapture.StartAsync();
                    vm.SetCameraStarted(true);
                }
                catch (Exception ex)
                {
                    vm.SetCameraStarted(false);
                    vm.ReportCameraError($"Could not start camera: {ex.Message}");
                }
            }
            else
            {
                CameraPreviewHost.Content = null;
                vm.SetCameraStarted(false);
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