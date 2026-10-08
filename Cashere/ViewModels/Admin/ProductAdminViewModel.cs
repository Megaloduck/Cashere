using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Cashere.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using QRCoder;

namespace Cashere.ViewModels.Admin;

public partial class ProductAdminViewModel : ViewModelBase
{
    private readonly IProductAdminService _productAdmin;
    private readonly ICategoryAdminService _categoryAdmin;
    private readonly IShopContextService? _shopContext;
    private readonly IProductDataTransferService? _dataTransfer;
    private readonly UserRole _currentRole;

    private List<Product> _allProducts = new();
    private int? _editingProductId;
    private int _defaultLowStockThreshold = 5;
    private byte[]? _formPhotoData;
    private string? _formPhotoExtension;

    public ObservableCollection<Product> FilteredProducts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    // Drives visibility of every mutating control on this screen (NEW
    // PRODUCT, Edit, Activate/Deactivate, Remove photo) - Cashier logins see
    // the same product list but none of these. Guarded again inside each
    // command below as defense-in-depth, same philosophy as SaleService
    // re-checking settings CheckoutViewModel already gates on.
    public bool CanManage => RolePermissions.CanManage(_currentRole);

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty] private bool _isDataTransferBusy;
    [ObservableProperty] private string? _dataTransferStatus;
    [ObservableProperty] private bool _updateExistingProducts;

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private string _editorTitle = "NEW PRODUCT";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty] private string _formSku = string.Empty;
    [ObservableProperty] private string _formBarcode = string.Empty;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private Category? _formCategory;
    [ObservableProperty] private string _formTaxRateOverridePercent = string.Empty;
    [ObservableProperty] private string _formUnit = "pcs";
    [ObservableProperty] private string _formCostPrice = "0";
    [ObservableProperty] private string _formSellingPrice = "0";
    [ObservableProperty] private string _formStockQuantity = "0";
    [ObservableProperty] private string _formLowStockThreshold = "5";
    [ObservableProperty] private OutOfStockOption _formOutOfStockOption = new("Use shop default", null);
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private Bitmap? _formPhotoPreview;
    [ObservableProperty] private Bitmap? _formQrPreview;
    [ObservableProperty] private string _formQrIdentity = string.Empty;

    public bool HasFormPhotoPreview => FormPhotoPreview is not null;
    public bool HasFormQrPreview => FormQrPreview is not null;
    public IReadOnlyList<OutOfStockOption> OutOfStockOptions { get; } =
    [
        new OutOfStockOption("Use shop default", null),
        new OutOfStockOption("Block sale when out of stock", OutOfStockBehavior.Block),
        new OutOfStockOption("Allow negative stock", OutOfStockBehavior.AllowNegativeStock)
    ];

    // Reflect Settings -> Inventory, refreshed on every LoadAsync() so this
    // screen never needs its own reload-on-nav wiring - it simply picks up
    // whatever was true the last time an admin visited Products.
    [ObservableProperty] private bool _autoGenerateSkuEnabled;
    [ObservableProperty] private bool _autoGenerateBarcodeEnabled;

    public ProductAdminViewModel(
        IProductAdminService productAdmin,
        ICategoryAdminService categoryAdmin,
        UserRole currentRole,
        IShopContextService? shopContext = null,
        IProductDataTransferService? dataTransfer = null)
    {
        _productAdmin = productAdmin;
        _categoryAdmin = categoryAdmin;
        _currentRole = currentRole;
        _shopContext = shopContext;
        _dataTransfer = dataTransfer;
    }

    public async Task LoadAsync()
    {
        var categories = await _categoryAdmin.GetAllCategoriesAsync();
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);


        if (_shopContext is not null)
        {
            var settings = await _shopContext.GetSettingsAsync();
            _defaultLowStockThreshold = settings?.DefaultLowStockThreshold ?? 5;
            AutoGenerateSkuEnabled = settings?.AutoGenerateSku ?? false;
            AutoGenerateBarcodeEnabled = settings?.AutoGenerateBarcode ?? false;
        }

        _allProducts = await _productAdmin.GetAllProductsAsync();
        ApplyFilter();
    }

    public async Task<string?> ExportProductsCsvAsync()
    {
        if (!CanManage || _dataTransfer is null) return null;
        IsDataTransferBusy = true;
        DataTransferStatus = null;
        try
        {
            return await _dataTransfer.ExportCsvAsync();
        }
        catch (Exception ex)
        {
            DataTransferStatus = $"Catalog export failed: {ex.Message}";
            return null;
        }
        finally
        {
            IsDataTransferBusy = false;
        }
    }

    public async Task ImportProductsCsvAsync(string csvContents)
    {
        if (!CanManage || _dataTransfer is null) return;
        IsDataTransferBusy = true;
        DataTransferStatus = null;
        try
        {
            var result = await _dataTransfer.ImportCsvAsync(csvContents, UpdateExistingProducts);
            await LoadAsync();
            DataTransferStatus = $"Import complete: {result.Added} added, {result.Updated} updated, {result.Skipped} skipped." +
                                 (result.Details is null ? string.Empty : Environment.NewLine + result.Details);
        }
        catch (Exception ex)
        {
            DataTransferStatus = $"Catalog import failed: {ex.Message}";
        }
        finally
        {
            IsDataTransferBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<Product> query = _allProducts;
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
    private void AddNew()
    {
        if (!CanManage) return;

        _editingProductId = null;
        _formPhotoData = null;
        _formPhotoExtension = null;
        FormQrIdentity = string.Empty;
        SetFormQrPreview(null);
        EditorTitle = "NEW PRODUCT";
        FormSku = string.Empty;
        FormBarcode = string.Empty;
        FormName = string.Empty;
        FormCategory = null;
        FormTaxRateOverridePercent = string.Empty;
        FormUnit = "pcs";
        FormCostPrice = "0";
        FormSellingPrice = "0";
        FormStockQuantity = "0";
        FormLowStockThreshold = _defaultLowStockThreshold.ToString();
        FormOutOfStockOption = OutOfStockOptions[0];
        SetFormPhotoPreview(null);
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditProduct(Product? product)
    {
        if (!CanManage || product is null) return;

        _editingProductId = product.Id;
        EditorTitle = "EDIT PRODUCT";
        FormSku = product.Sku;
        FormBarcode = product.Barcode ?? string.Empty;
        FormName = product.Name;
        FormCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        FormTaxRateOverridePercent = product.TaxRateOverridePercent?.ToString() ?? string.Empty;
        FormUnit = product.Unit;
        FormCostPrice = product.CostPrice.ToString();
        FormSellingPrice = product.SellingPrice.ToString();
        FormStockQuantity = product.StockQuantity.ToString();
        FormLowStockThreshold = product.LowStockThreshold.ToString();
        FormOutOfStockOption = OutOfStockOptions.First(option => option.Value == product.OutOfStockBehaviorOverride);
        _formPhotoData = null;
        _formPhotoExtension = null;
        LoadExistingPhotoPreview(product.PhotoPath);
        FormQrIdentity = ProductQrIdentity.ForProductId(product.Id);
        SetFormQrPreview(CreateQrPreview(FormQrIdentity));
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    public async Task SetFormPhotoAsync(byte[] imageData, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
        {
            ErrorMessage = "Choose a PNG, JPG, JPEG, or WebP image.";
            return;
        }
        if (imageData.Length == 0 || imageData.Length > 10 * 1024 * 1024)
        {
            ErrorMessage = "The image must be smaller than 10 MB.";
            return;
        }

        try
        {
            using var stream = new MemoryStream(imageData);
            var preview = new Bitmap(stream);
            SetFormPhotoPreview(preview);
            _formPhotoData = imageData;
            _formPhotoExtension = extension;
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
        {
            ErrorMessage = "The selected file is not a valid image.";
        }
        await Task.CompletedTask;
    }

    private void LoadExistingPhotoPreview(string? relativePath)
    {
        try
        {
            var fullPath = string.IsNullOrWhiteSpace(relativePath)
                ? null
                : Cashere.Converters.PhotoPathToImageConverter.GetFullPath(relativePath);
            SetFormPhotoPreview(fullPath is not null && File.Exists(fullPath) ? new Bitmap(fullPath) : null);
        }
        catch
        {
            SetFormPhotoPreview(null);
        }
    }

    private void SetFormPhotoPreview(Bitmap? preview)
    {
        FormPhotoPreview?.Dispose();
        FormPhotoPreview = preview;
        OnPropertyChanged(nameof(HasFormPhotoPreview));
    }

    private static Bitmap? CreateQrPreview(string identity)
    {
        if (string.IsNullOrEmpty(identity)) return null;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(identity, QRCodeGenerator.ECCLevel.M);
        using var stream = new MemoryStream(new PngByteQRCode(data).GetGraphic(8));
        return new Bitmap(stream);
    }

    private void SetFormQrPreview(Bitmap? preview)
    {
        FormQrPreview?.Dispose();
        FormQrPreview = preview;
        OnPropertyChanged(nameof(HasFormQrPreview));
    }

    private async Task SaveFormPhotoAsync(int productId)
    {
        if (_formPhotoData is null || _formPhotoExtension is null) return;

        var relativePath = $"products/{productId}{_formPhotoExtension}";
        var fullPath = Cashere.Converters.PhotoPathToImageConverter.GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".tmp";
        await File.WriteAllBytesAsync(temporaryPath, _formPhotoData);
        File.Move(temporaryPath, fullPath, overwrite: true);
        await _productAdmin.SetPhotoPathAsync(productId, relativePath);
        _formPhotoData = null;
        _formPhotoExtension = null;
    }

    [RelayCommand]
    private async Task ToggleActive(Product? product)
    {
        if (!CanManage || product is null) return;
        await _productAdmin.SetActiveAsync(product.Id, !product.IsActive);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RemovePhoto(Product? product)
    {
        if (!CanManage || product is null) return;
        await _productAdmin.SetPhotoPathAsync(product.Id, null);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        if (!CanManage || string.IsNullOrWhiteSpace(NewCategoryName)) return;

        try
        {
            var category = await _categoryAdmin.CreateCategoryAsync(NewCategoryName.Trim());
            Categories.Add(category);
            FormCategory = category;
            NewCategoryName = string.Empty;
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanManage) return;

        ErrorMessage = null;

        var isNewProduct = _editingProductId is null;
        var skuRequired = !(isNewProduct && AutoGenerateSkuEnabled);

        if ((skuRequired && string.IsNullOrWhiteSpace(FormSku)) || string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = skuRequired ? "SKU and name are required." : "Name is required.";
            return;
        }

        if (!decimal.TryParse(FormCostPrice, out var costPrice) ||
            !decimal.TryParse(FormSellingPrice, out var sellingPrice) ||
            !int.TryParse(FormStockQuantity, out var stockQuantity) ||
            !int.TryParse(FormLowStockThreshold, out var lowStockThreshold))
        {
            ErrorMessage = "Check that price, stock and threshold are valid numbers.";
            return;
        }

        decimal? taxRateOverridePercent = null;
        if (!string.IsNullOrWhiteSpace(FormTaxRateOverridePercent))
        {
            if (!decimal.TryParse(FormTaxRateOverridePercent, out var parsedTaxRate) || parsedTaxRate is < 0 or > 100)
            {
                ErrorMessage = "Product tax override must be a percentage from 0 to 100, or left blank.";
                return;
            }
            taxRateOverridePercent = parsedTaxRate;
        }

        var input = new ProductInput(
            FormSku, string.IsNullOrWhiteSpace(FormBarcode) ? null : FormBarcode,
            FormName, FormCategory?.Id, FormUnit, costPrice, sellingPrice, stockQuantity, lowStockThreshold,
            FormOutOfStockOption.Value, taxRateOverridePercent);

        try
        {
            int savedProductId;
            if (_editingProductId is int id)
            {
                await _productAdmin.UpdateProductAsync(id, input);
                savedProductId = id;
            }
            else
            {
                var product = await _productAdmin.CreateProductAsync(input);
                savedProductId = product.Id;
                _editingProductId = product.Id;
            }

            await SaveFormPhotoAsync(savedProductId);
            IsEditorOpen = false;
            await LoadAsync();
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (IOException ex)
        {
            ErrorMessage = $"Product saved, but the photo could not be saved: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _formPhotoData = null;
        _formPhotoExtension = null;
        SetFormPhotoPreview(null);
        FormQrIdentity = string.Empty;
        SetFormQrPreview(null);
        IsEditorOpen = false;
    }
}
