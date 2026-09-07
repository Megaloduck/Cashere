using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin;

public partial class PurchaseAdminViewModel : ViewModelBase
{
    private readonly IPurchaseAdminService _purchaseAdmin;
    private readonly ISupplierAdminService _supplierAdmin;
    private readonly IProductCatalogService _productCatalog;
    private readonly int _cashierId;

    public ObservableCollection<Purchase> Purchases { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<PurchaseLineViewModel> FormLines { get; } = new();

    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private Supplier? _formSupplier;
    [ObservableProperty] private string _formReferenceNumber = string.Empty;
    [ObservableProperty] private string _formNotes = string.Empty;

    [ObservableProperty] private Product? _lineProduct;
    [ObservableProperty] private string _lineQuantity = "1";
    [ObservableProperty] private string _lineUnitCost = "0";

    public decimal FormTotal => FormLines.Sum(l => l.Subtotal);

    public PurchaseAdminViewModel(
        IPurchaseAdminService purchaseAdmin,
        ISupplierAdminService supplierAdmin,
        IProductCatalogService productCatalog,
        int cashierId)
    {
        _purchaseAdmin = purchaseAdmin;
        _supplierAdmin = supplierAdmin;
        _productCatalog = productCatalog;
        _cashierId = cashierId;
    }

    public async Task LoadAsync()
    {
        var purchases = await _purchaseAdmin.GetAllPurchasesAsync();
        Purchases.Clear();
        foreach (var purchase in purchases) Purchases.Add(purchase);

        var suppliers = await _supplierAdmin.GetAllSuppliersAsync();
        Suppliers.Clear();
        foreach (var supplier in suppliers) Suppliers.Add(supplier);

        var products = await _productCatalog.GetActiveProductsAsync();
        Products.Clear();
        foreach (var product in products) Products.Add(product);
    }

    [RelayCommand]
    private void AddNew()
    {
        FormSupplier = null;
        FormReferenceNumber = string.Empty;
        FormNotes = string.Empty;
        LineProduct = null;
        LineQuantity = "1";
        LineUnitCost = "0";
        FormLines.Clear();
        ErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void AddLine()
    {
        ErrorMessage = null;

        if (LineProduct is null)
        {
            ErrorMessage = "Choose a product to add.";
            return;
        }

        if (!int.TryParse(LineQuantity, out var quantity) || quantity <= 0)
        {
            ErrorMessage = "Quantity must be a whole number greater than zero.";
            return;
        }

        if (!decimal.TryParse(LineUnitCost, out var unitCost) || unitCost < 0)
        {
            ErrorMessage = "Unit cost must be a valid number.";
            return;
        }

        // Deliberately not merged with an existing line for the same product -
        // a single purchase can legitimately receive the same product at two
        // different costs (e.g. a price change mid-batch).
        FormLines.Add(new PurchaseLineViewModel(LineProduct.Id, LineProduct.Name, quantity, unitCost));
        OnPropertyChanged(nameof(FormTotal));

        LineProduct = null;
        LineQuantity = "1";
        LineUnitCost = "0";
    }

    [RelayCommand]
    private void RemoveLine(PurchaseLineViewModel? line)
    {
        if (line is null) return;
        FormLines.Remove(line);
        OnPropertyChanged(nameof(FormTotal));
    }

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = null;

        if (FormSupplier is null)
        {
            ErrorMessage = "Select a supplier.";
            return;
        }

        if (FormLines.Count == 0)
        {
            ErrorMessage = "Add at least one line item.";
            return;
        }

        var input = new PurchaseInput(
            FormSupplier.Id,
            _cashierId,
            string.IsNullOrWhiteSpace(FormReferenceNumber) ? null : FormReferenceNumber,
            string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes,
            FormLines.Select(l => new PurchaseLineInput(l.ProductId, l.Quantity, l.UnitCost)).ToList());

        try
        {
            await _purchaseAdmin.CreatePurchaseAsync(input);
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