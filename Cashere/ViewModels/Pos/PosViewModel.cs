using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Cashere.ViewModels.Pos;

public partial class PosViewModel : ViewModelBase
{
    private readonly IProductCatalogService _catalog;
    private readonly ISaleService _saleService;

    public ProductPickerViewModel ProductPicker { get; }
    public CartViewModel Cart { get; }

    public int CurrentCashierId { get; }
    public string CurrentCashierName { get; }

    // Raised when the cashier taps ADMIN in the header - ShellViewModel
    // subscribes to swap the current screen without PosViewModel needing to
    // know anything about navigation itself.
    public event Action? AdminRequested;

    [ObservableProperty]
    private CheckoutViewModel? _checkout;

    [ObservableProperty]
    private bool _isCheckoutOpen;

    [ObservableProperty]
    private string _lastReceiptSummary = string.Empty;

    public PosViewModel(
        IProductCatalogService catalog,
        ISaleService saleService,
        decimal taxRatePercent,
        int cashierId,
        string cashierName)
    {
        _catalog = catalog;
        _saleService = saleService;
        CurrentCashierId = cashierId;
        CurrentCashierName = cashierName;

        ProductPicker = new ProductPickerViewModel(catalog);
        ProductPicker.ProductSelected += OnProductSelected;

        Cart = new CartViewModel { TaxRatePercent = taxRatePercent };
    }

    public async Task InitializeAsync()
    {
        await ProductPicker.LoadAsync();
    }

    private void OnProductSelected(Product product)
    {
        Cart.AddProduct(product);
    }

    [RelayCommand]
    private void OpenCheckout()
    {
        if (!Cart.HasItems) return;

        Checkout = new CheckoutViewModel(_saleService, Cart, CurrentCashierId);
        Checkout.SaleCompleted += OnSaleCompleted;
        Checkout.Cancelled += OnCheckoutCancelled;
        IsCheckoutOpen = true;
    }

    [RelayCommand]
    private void OpenAdmin() => AdminRequested?.Invoke();

    private async void OnSaleCompleted(CompletedSaleResult result)
    {
        LastReceiptSummary =
            $"Sale {result.SaleNumber} complete - total Rp {result.TotalAmount:N0}, change Rp {result.ChangeDue:N0}";

        CloseCheckout();
        Cart.Clear();
        await ProductPicker.RefreshProductsAsync();
    }

    private void OnCheckoutCancelled()
    {
        CloseCheckout();
    }

    private void CloseCheckout()
    {
        IsCheckoutOpen = false;
        if (Checkout is not null)
        {
            Checkout.SaleCompleted -= OnSaleCompleted;
            Checkout.Cancelled -= OnCheckoutCancelled;
        }
        Checkout = null;
    }
}