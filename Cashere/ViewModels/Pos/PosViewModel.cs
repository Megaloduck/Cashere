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
        TaxAndRoundingSettings taxSettings,
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

        Cart = new CartViewModel(voucherAdmin)
        {
            TaxRatePercent = taxSettings.DefaultTaxRatePercent,
            PricesIncludeTax = taxSettings.PricesIncludeTax,
            RoundingMode = taxSettings.RoundingMode,
            RoundingIncrement = taxSettings.RoundingIncrement
        };
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

    // Picks up any Settings -> Business Info change (default rate, rounding,
    // tax-inclusive toggle) the moment the cashier returns to the till - same
    // "refresh on ShowPos" pattern as RefreshPaymentSettingsAsync above.
    // Lines already sitting in an open cart keep whatever rate they resolved
    // to at add-time (see CartLineViewModel.TaxRatePercent); only the
    // shop-wide default and the rounding/inclusive settings change here.
    public async Task RefreshTaxSettingsAsync()
    {
        var taxSettings = await _shopContext.GetTaxAndRoundingSettingsAsync();
        Cart.TaxRatePercent = taxSettings.DefaultTaxRatePercent;
        Cart.PricesIncludeTax = taxSettings.PricesIncludeTax;
        Cart.RoundingMode = taxSettings.RoundingMode;
        Cart.RoundingIncrement = taxSettings.RoundingIncrement;
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

            // Recomputed here (before Cart.Clear() below) via the same
            // TaxCalculator SaleService just used, grouped by rate, so the
            // printed receipt can show a per-rate breakdown when a sale
            // mixed more than one tax rate, or a single "TAX (X%)" line
            // when it didn't - see SaleReceiptFormatter.Format.
            var taxCalculation = TaxCalculator.Calculate(
                Cart.Lines.Select(l => new TaxCalculator.LineInput(l.Subtotal, l.TaxRatePercent)).ToList(),
                Cart.DiscountAmount, Cart.PricesIncludeTax);

            var taxBreakdown = taxCalculation.Lines
                .GroupBy(l => l.RatePercent)
                .Select(g => (RatePercent: g.Key, Amount: g.Sum(l => l.TaxAmount)))
                .OrderByDescending(g => g.RatePercent)
                .ToList();

            var checkoutSnapshot = Checkout;
            _ = TryAutoPrintReceiptAsync(result, checkoutSnapshot, lines, taxBreakdown);
        }

        CloseCheckout();
        Cart.Clear();
        await ProductPicker.RefreshProductsAsync();
    }

    private async Task TryAutoPrintReceiptAsync(
        CompletedSaleResult result,
        CheckoutViewModel checkout,
        IReadOnlyList<SaleReceiptLine> lines,
        IReadOnlyList<(decimal RatePercent, decimal Amount)> taxBreakdown)
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
                taxBreakdown,
                result.RoundingAdjustment,
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
