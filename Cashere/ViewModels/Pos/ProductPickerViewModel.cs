using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Models;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Pos;

public partial class ProductPickerViewModel : ViewModelBase
{
    private readonly IProductCatalogService _catalog;
    private List<Product> _allProducts = new();

    public ObservableCollection<Product> FilteredProducts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    // Raised when the user taps a product card; PosViewModel subscribes and
    // forwards the product into the cart.
    public event Action<Product>? ProductSelected;

    public ProductPickerViewModel(IProductCatalogService catalog)
    {
        _catalog = catalog;
    }

    public async Task LoadAsync()
    {
        var categories = await _catalog.GetCategoriesAsync();
        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        await RefreshProductsAsync();
    }

    public async Task RefreshProductsAsync()
    {
        _allProducts = await _catalog.GetActiveProductsAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryChanged(Category? value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<Product> query = _allProducts;

        if (SelectedCategory is not null)
        {
            var categoryId = SelectedCategory.Id;
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode is not null && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredProducts.Clear();
        foreach (var product in query)
        {
            FilteredProducts.Add(product);
        }
    }

    [RelayCommand]
    private void SelectProduct(Product? product)
    {
        if (product is null) return;
        ProductSelected?.Invoke(product);
    }
}
