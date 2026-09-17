using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Cashere.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Admin;

public partial class ProductAdminViewModel : ViewModelBase
{
    private readonly IProductAdminService _productAdmin;
    private readonly ICategoryAdminService _categoryAdmin;
    private readonly IShopContextService? _shopContext;
    private readonly UserRole _currentRole;

    private List<Product> _allProducts = new();
    private int? _editingProductId;
    private int _defaultLowStockThreshold = 5;

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
    [ObservableProperty] private string _formUnit = "pcs";
    [ObservableProperty] private string _formCostPrice = "0";
    [ObservableProperty] private string _formSellingPrice = "0";
    [ObservableProperty] private string _formStockQuantity = "0";
    [ObservableProperty] private string _formLowStockThreshold = "5";
    [ObservableProperty] private string _newCategoryName = string.Empty;

    // Reflect Settings -> Inventory, refreshed on every LoadAsync() so this
    // screen never needs its own reload-on-nav wiring - it simply picks up
    // whatever was true the last time an admin visited Products.
    [ObservableProperty] private bool _autoGenerateSkuEnabled;
    [ObservableProperty] private bool _autoGenerateBarcodeEnabled;

    public ProductAdminViewModel(
        IProductAdminService productAdmin,
        ICategoryAdminService categoryAdmin,
        UserRole currentRole,
        IShopContextService? shopContext = null)
    {
        _productAdmin = productAdmin;
        _categoryAdmin = categoryAdmin;
        _currentRole = currentRole;
        _shopContext = shopContext;
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
        EditorTitle = "NEW PRODUCT";
        FormSku = string.Empty;
        FormBarcode = string.Empty;
        FormName = string.Empty;
        FormCategory = null;
        FormUnit = "pcs";
        FormCostPrice = "0";
        FormSellingPrice = "0";
        FormStockQuantity = "0";
        FormLowStockThreshold = _defaultLowStockThreshold.ToString();
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
        FormUnit = product.Unit;
        FormCostPrice = product.CostPrice.ToString();
        FormSellingPrice = product.SellingPrice.ToString();
        FormStockQuantity = product.StockQuantity.ToString();
        FormLowStockThreshold = product.LowStockThreshold.ToString();
        ErrorMessage = null;
        IsEditorOpen = true;
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

        var input = new ProductInput(
            FormSku, string.IsNullOrWhiteSpace(FormBarcode) ? null : FormBarcode,
            FormName, FormCategory?.Id, FormUnit, costPrice, sellingPrice, stockQuantity, lowStockThreshold);

        try
        {
            if (_editingProductId is int id)
            {
                await _productAdmin.UpdateProductAsync(id, input);
            }
            else
            {
                await _productAdmin.CreateProductAsync(input);
            }

            IsEditorOpen = false;
            await LoadAsync();
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelEdit() => IsEditorOpen = false;
}