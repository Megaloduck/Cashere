using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Cashere.Formatting;

namespace Cashere.ViewModels.Pos;

public partial class PosViewModel : ViewModelBase
{
    private readonly IProductCatalogService _catalog;
    private readonly ISaleService _saleService;
    private readonly IShopContextService _shopContext;
    private readonly ICustomerAdminService? _customerAdmin;
    private readonly IReceiptPrinterService? _receiptPrinter;

    private PaymentSettings _paymentSettings = new(true, true, true, null, null, false);
    private SalesBehaviorSettings _salesBehaviorSettings = new(false, false);
    private List<Customer> _customers = new();

    public ProductPickerViewModel ProductPicker { get; }
    public CartViewModel Cart { get; }

    public int CurrentCashierId { get; }
    public string CurrentCashierName { get; }
    public HeaderClockViewModel HeaderClock => HeaderClockService.Current;

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
        IShopContextService shopContext,
        decimal taxRatePercent,
        int cashierId,
        string cashierName,
        ICustomerAdminService? customerAdmin = null,
        IReceiptPrinterService? receiptPrinter = null,
        IVoucherAdminService? voucherAdmin = null)
    {
        _catalog = catalog;
        _saleService = saleService;
        _shopContext = shopContext;
        _customerAdmin = customerAdmin;
        _receiptPrinter = receiptPrinter;
        CurrentCashierId = cashierId;
        CurrentCashierName = cashierName;

        ProductPicker = new ProductPickerViewModel(catalog);
        ProductPicker.ProductSelected += OnProductSelected;

        Cart = new CartViewModel(voucherAdmin) { TaxRatePercent = taxRatePercent };
    }

    public async Task InitializeAsync()
    {
        await ProductPicker.LoadAsync();
        await RefreshPaymentSettingsAsync();
        await RefreshSalesBehaviorSettingsAsync();
        await RefreshCustomersAsync();
    }

    public async Task RefreshPaymentSettingsAsync()
    {
        _paymentSettings = await _shopContext.GetPaymentSettingsAsync();
    }

    public async Task RefreshSalesBehaviorSettingsAsync()
    {
        _salesBehaviorSettings = await _shopContext.GetSalesBehaviorSettingsAsync();
    }

    public async Task RefreshCustomersAsync()
    {
        if (_customerAdmin is null) return;
        _customers = await _customerAdmin.GetAllCustomersAsync();
    }

    private void OnProductSelected(Product product)
    {
        Cart.AddProduct(product);
    }

    [RelayCommand]
    private void OpenCheckout()
    {
        if (!Cart.HasItems) return;

        Checkout = new CheckoutViewModel(
            _saleService, Cart, CurrentCashierId, _paymentSettings, _salesBehaviorSettings, _customers);
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

        if (_salesBehaviorSettings.AutoPrintReceiptAfterPayment && _receiptPrinter is not null && Checkout is not null)
        {
            var lines = Cart.Lines.Select(l => new SaleReceiptLine(l.Name, l.Quantity, l.Subtotal)).ToList();
            var checkoutSnapshot = Checkout;
            var taxRatePercent = Cart.TaxRatePercent;
            _ = TryAutoPrintReceiptAsync(result, checkoutSnapshot, lines, taxRatePercent);
        }

        CloseCheckout();
        Cart.Clear();
        await ProductPicker.RefreshProductsAsync();
    }

    private async Task TryAutoPrintReceiptAsync(
        CompletedSaleResult result,
        CheckoutViewModel checkout,
        IReadOnlyList<SaleReceiptLine> lines,
        decimal taxRatePercent)
    {
        try
        {
            var settings = await _shopContext.GetSettingsAsync();
            var currency = string.IsNullOrWhiteSpace(settings?.Currency) ? "IDR" : settings.Currency;

            var context = new SaleReceiptContext(
                settings?.ShopName ?? "Cashere",
                settings?.Address,
                settings?.Phone,
                currency,
                result.SaleDate,
                result.SaleNumber,
                CurrentCashierName,
                lines,
                result.Subtotal,
                result.DiscountAmount,
                taxRatePercent,
                result.TaxAmount,
                result.TotalAmount,
                checkout.IsCashPayment,
                checkout.PaymentMethod.ToString(),
                checkout.AmountTendered,
                checkout.ChangeDue,
                checkout.ReferenceNumber,
                settings?.ReceiptFooterText);

            await _receiptPrinter!.PrintAsync(SaleReceiptFormatter.Format(context));
        }
        catch
        {
        }
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