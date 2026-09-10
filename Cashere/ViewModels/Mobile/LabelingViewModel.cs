using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using Cashere.Services;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Mobile;

// Product-photo-labeling flow: pick a product from the connected till's
// catalog, capture a still photo, review it, then upload. Mirrors the
// connect-once/act-many shape of ScanningViewModel - owned for the app's
// whole lifetime by MobileShellViewModel, so state survives switching tabs.
public partial class LabelingViewModel : ViewModelBase
{
    private readonly IPosSyncClientService _syncClient;
    private readonly IPhotoCaptureService? _photoCapture;
    private readonly IProductPhotoService? _productPhoto;

    private List<ProductLookupItem> _allProducts = new();

    // Exposed so the View's code-behind can create/host the native preview
    // control - same pattern as ScanningViewModel.Scanner.
    public IPhotoCaptureService? PhotoCapture => _photoCapture;

    public ObservableCollection<ProductLookupItem> FilteredProducts { get; } = new();

    // Raised whenever ShowCameraPreview flips, so the View knows to
    // attach/detach the native preview control.
    public event Action<bool>? CameraReadyChanged;

    [ObservableProperty]
    private SyncConnectionState _state = SyncConnectionState.Disconnected;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ProductLookupItem? _selectedProduct;

    [ObservableProperty]
    private CameraPermissionStatus _cameraPermission = CameraPermissionStatus.Unknown;

    [ObservableProperty]
    private bool _isCameraStarted;

    [ObservableProperty]
    private byte[]? _capturedPhoto;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    // Computed alongside CapturedPhoto in OnCapturedPhotoChanged below rather
    // than as an [ObservableProperty], since it needs a manual Dispose() of
    // the previous Bitmap before creating the next one.
    public Bitmap? CapturedImage { get; private set; }

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool ShowProductList => IsConnected && SelectedProduct is null;

    // Called by the View once PhotoCapture.StartAsync() actually completes -
    // ShowCameraPreview turning true only means permission is granted and the
    // capture panel is showing, not that the native camera session is bound
    // yet, so TAKE PHOTO stays disabled until this confirms it.
    public void SetCameraStarted(bool started) => IsCameraStarted = started;

    public void ReportCameraError(string message) => StatusMessage = message;
    public bool ShowCapturePanel => IsConnected && SelectedProduct is not null && CapturedPhoto is null;
    public bool ShowReviewPanel => IsConnected && SelectedProduct is not null && CapturedPhoto is not null;
    public bool ShowCameraPreview => ShowCapturePanel && CameraPermission == CameraPermissionStatus.Granted;
    public bool CameraPermissionDenied => CameraPermission == CameraPermissionStatus.Denied;

    public LabelingViewModel(
        IPosSyncClientService syncClient,
        IPhotoCaptureService? photoCapture = null,
        IProductPhotoService? productPhoto = null)
    {
        _syncClient = syncClient;
        _photoCapture = photoCapture;
        _productPhoto = productPhoto;

        _syncClient.StateChanged += HandleSyncStateChanged;
        _syncClient.ProductCatalogChanged += HandleProductCatalogChanged;
        State = _syncClient.State;

        if (_photoCapture is not null)
        {
            CameraPermission = _photoCapture.PermissionStatus;
        }
    }

    // Called by MobileShellViewModel whenever this section becomes visible,
    // by the push handler below, and by the manual REFRESH button in the
    // product list header.
    public async Task RefreshAsync()
    {
        if (!IsConnected || _productPhoto is null) return;

        try
        {
            _allProducts = (await _productPhoto.GetProductsAsync()).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load products: {ex.Message}";
        }
    }

    // The till pushes this whenever desktop admin creates/edits/(de)activates
    // a product, uploads/removes a photo, or receives a purchase that changed
    // stock - keeps the product list fresh without the cashier needing to
    // leave and re-enter the tab or tap the manual refresh button themselves.
    private void HandleProductCatalogChanged() => _ = RefreshAsync();

    private async void HandleSyncStateChanged(SyncConnectionState state)
    {
        State = state;

        if (state != SyncConnectionState.Connected)
        {
            // Connection dropped mid-flow - reset everything rather than
            // leaving a stale product/photo selected against a till we're
            // no longer talking to.
            SelectedProduct = null;
            CapturedPhoto = null;
            _allProducts.Clear();
            FilteredProducts.Clear();

            if (_photoCapture is not null)
            {
                await _photoCapture.StopAsync();
            }
        }
    }

    partial void OnStateChanged(SyncConnectionState value)
    {
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ShowProductList));
        OnPropertyChanged(nameof(ShowCapturePanel));
        OnPropertyChanged(nameof(ShowReviewPanel));
        OnPropertyChanged(nameof(ShowCameraPreview));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedProductChanged(ProductLookupItem? value)
    {
        OnPropertyChanged(nameof(ShowProductList));
        OnPropertyChanged(nameof(ShowCapturePanel));
        OnPropertyChanged(nameof(ShowReviewPanel));
        OnPropertyChanged(nameof(ShowCameraPreview));
        StatusMessage = null;
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    partial void OnCapturedPhotoChanged(byte[]? value)
    {
        CapturedImage?.Dispose();
        CapturedImage = value is null ? null : new Bitmap(new MemoryStream(value));
        OnPropertyChanged(nameof(CapturedImage));
        OnPropertyChanged(nameof(ShowCapturePanel));
        OnPropertyChanged(nameof(ShowReviewPanel));
        OnPropertyChanged(nameof(ShowCameraPreview));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    partial void OnCameraPermissionChanged(CameraPermissionStatus value)
    {
        OnPropertyChanged(nameof(ShowCameraPreview));
        OnPropertyChanged(nameof(CameraPermissionDenied));
        CameraReadyChanged?.Invoke(ShowCameraPreview);
    }

    private void ApplyFilter()
    {
        IEnumerable<ProductLookupItem> query = _allProducts;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode is not null && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredProducts.Clear();
        foreach (var product in query) FilteredProducts.Add(product);
    }

    [RelayCommand]
    private void SelectProduct(ProductLookupItem? product)
    {
        if (product is null) return;
        SelectedProduct = product;
    }

    [RelayCommand]
    private async Task BackToList()
    {
        if (_photoCapture is not null)
        {
            await _photoCapture.StopAsync();
        }
        SelectedProduct = null;
        CapturedPhoto = null;
        StatusMessage = null;
    }

    [RelayCommand]
    private async Task Refresh() => await RefreshAsync();

    [RelayCommand]
    private async Task Capture()
    {
        if (_photoCapture is null || !IsCameraStarted) return;

        try
        {
            CapturedPhoto = await _photoCapture.CapturePhotoAsync();
            await _photoCapture.StopAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Capture failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EnableCamera()
    {
        if (_photoCapture is null) return;
        CameraPermission = await _photoCapture.RequestCameraPermissionAsync();
    }

    // Clearing CapturedPhoto flips ShowCameraPreview back to true (permission
    // already granted from before), which raises CameraReadyChanged - the
    // View reacts to that by re-attaching the preview and calling
    // PhotoCapture.StartAsync(), same as it did the first time around.
    [RelayCommand]
    private void Retake()
    {
        CapturedPhoto = null;
        StatusMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmUpload()
    {
        if (SelectedProduct is null || CapturedPhoto is null || _productPhoto is null) return;

        IsBusy = true;
        StatusMessage = null;
        try
        {
            var fileName = $"{SelectedProduct.Id}.jpg";
            var outcome = await _productPhoto.UploadPhotoAsync(SelectedProduct.Id, CapturedPhoto, fileName);

            if (outcome.Success)
            {
                var justUploaded = SelectedProduct;

                var index = _allProducts.FindIndex(p => p.Id == justUploaded.Id);
                if (index >= 0)
                {
                    _allProducts[index] = _allProducts[index] with { HasPhoto = true };
                }

                SelectedProduct = null;
                CapturedPhoto = null;
                ApplyFilter();
                StatusMessage = $"Photo saved for {justUploaded.Name}.";
            }
            else
            {
                StatusMessage = outcome.ErrorMessage ?? "Upload failed - try again.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}