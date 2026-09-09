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

    private List<Product> _allProducts = new();
    private int? _editingProductId;

    public ObservableCollection<Product> FilteredProducts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private string _editorTitle = "NEW PRODUCT";

    [ObservableProperty]
    private string? _errorMessage;

    // Form fields are plain strings (not decimal/int) so a TextBox can hold
    // an empty or partially-typed value while editing; parsed on Save.
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

    public ProductAdminViewModel(IProductAdminService productAdmin, ICategoryAdminService categoryAdmin)
    {
        _productAdmin = productAdmin;
        _categoryAdmin = categoryAdmin;
    }

    public async Task LoadAsync()
    {
        var categories = await _categoryAdmin.GetAllCategoriesAsync();
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);

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
        FormLowStockThreshold = "5";
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditProduct(Product? product)
    {
        if (product is null) return;

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
        if (product is null) return;
        await _productAdmin.SetActiveAsync(product.Id, !product.IsActive);
        await LoadAsync();
    }

    // Manual escape hatch for a bad/wrong photo captured from the mobile
    // labeling flow - clears Product.PhotoPath so the tile/row falls back to
    // the category-colored placeholder. Doesn't touch the file on disk
    // (CashereServerHost's upload endpoint overwrites it on next upload
    // regardless), just the DB reference.
    [RelayCommand]
    private async Task RemovePhoto(Product? product)
    {
        if (product is null) return;
        await _productAdmin.SetPhotoPathAsync(product.Id, null);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

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
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FormSku) || string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = "SKU and name are required.";
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