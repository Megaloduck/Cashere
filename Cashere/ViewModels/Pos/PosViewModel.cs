using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Cashere.Formatting;

namespace Cashere.ViewModels.Pos;

public partial class PosViewModel : ViewModelBase
{
    private readonly IProductCatalogService _catalog;
    private readonly ISaleService _saleService;
    private readonly IShopContextService _shopContext;
    private readonly ICustomerAdminService? _customerAdmin;
    private readonly UserRole _cashierRole;
    private readonly IReceiptPrinterService? _receiptPrinter;
    private readonly IPosFeedbackService? _posFeedback;
    private readonly HeldOrderStore _heldOrderStore = new();

    private PaymentSettings _paymentSettings = new(true, true, true, null, null, false, 0, 0, 0);
    private SalesBehaviorSettings _salesBehaviorSettings = new(false, false, true);
    private List<Customer> _customers = new();

    public ProductPickerViewModel ProductPicker { get; }
    public CartViewModel Cart { get; }
    public ObservableCollection<HeldOrder> HeldOrders { get; } = new();
    public ObservableCollection<string> OrderTypes { get; } = new();
    public bool HasHeldOrders => HeldOrders.Count > 0;
    public bool HasMultipleOrderTypes => OrderTypes.Count > 1;
    public bool IsHeldOrdersEnabled => _salesBehaviorSettings.EnableHeldOrders;
    public bool ShowHeldOrders => IsHeldOrdersEnabled && HasHeldOrders;

    public int CurrentCashierId { get; }
    public string CurrentCashierName { get; }
    public HeaderClockViewModel HeaderClock => HeaderClockService.Current;

    public event Action? AdminRequested;

    [ObservableProperty]
    private CheckoutViewModel? _checkout;

    [ObservableProperty]
    private bool _isCheckoutOpen;

    [ObservableProperty]
    private bool _enableCustomerDisplay;

    [ObservableProperty]
    private string _selectedOrderType = "Sale";

    [ObservableProperty]
    private bool _posSoundsEnabled;

    [ObservableProperty]
    private bool _showPosNotifications = true;

    [ObservableProperty]
    private bool _enableCashDrawerKick;

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
        IVoucherAdminService? voucherAdmin = null,
        UserRole cashierRole = UserRole.Cashier,
        IPosFeedbackService? posFeedback = null)
    {
        _catalog = catalog;
        _saleService = saleService;
        _shopContext = shopContext;
        _customerAdmin = customerAdmin;
        _receiptPrinter = receiptPrinter;
        _posFeedback = posFeedback;
        _cashierRole = cashierRole;
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
        HeldOrders.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasHeldOrders));
            OnPropertyChanged(nameof(ShowHeldOrders));
        };
    }

    public async Task InitializeAsync()
    {
        try
        {
            foreach (var order in _heldOrderStore.Load())
            {
                HeldOrders.Add(order);
            }
        }
        catch (Exception ex)
        {
            LastReceiptSummary = $"Held orders could not be loaded: {ex.Message}";
        }

        var inventorySettings = await _shopContext.GetSettingsAsync();
        Cart.TrackInventory = inventorySettings?.TrackInventory ?? true;
        Cart.DefaultOutOfStockBehavior = inventorySettings?.OutOfStockBehavior ?? OutOfStockBehavior.Block;
        Cart.CanApplyVouchers = _cashierRole != UserRole.Cashier || inventorySettings?.CashierCanApplyVouchers != false;
        ProductPicker.AutoAddScannedBarcode = inventorySettings?.AutoAddScannedBarcode ?? true;
        EnableCustomerDisplay = inventorySettings?.EnableCustomerDisplay ?? false;
        PosSoundsEnabled = inventorySettings?.PosSoundsEnabled ?? false;
        ShowPosNotifications = inventorySettings?.ShowPosNotifications ?? true;
        EnableCashDrawerKick = inventorySettings?.EnableCashDrawerKick ?? false;
        ApplyOrderTypes(inventorySettings?.OrderTypes);

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
        OnPropertyChanged(nameof(IsHeldOrdersEnabled));
        OnPropertyChanged(nameof(ShowHeldOrders));
    }

    public async Task RefreshCustomersAsync()
    {
        if (_customerAdmin is null) return;
        _customers = await _customerAdmin.GetAllCustomersAsync();
    }

    public async Task RefreshScannerSettingsAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        ProductPicker.AutoAddScannedBarcode = settings?.AutoAddScannedBarcode ?? true;
        EnableCustomerDisplay = settings?.EnableCustomerDisplay ?? false;
        PosSoundsEnabled = settings?.PosSoundsEnabled ?? false;
        ShowPosNotifications = settings?.ShowPosNotifications ?? true;
        EnableCashDrawerKick = settings?.EnableCashDrawerKick ?? false;
        ApplyOrderTypes(settings?.OrderTypes);
    }

    // Picks up any Settings -> Payments change (default rate, rounding,
    // tax-inclusive toggle) the moment the cashier returns to the till - same
    // "refresh on ShowPos" pattern as RefreshPaymentSettingsAsync above.
    // Lines already sitting in an open cart keep whatever rate they resolved
    // to at add-time (see CartLineViewModel.TaxRatePercent); only the
    // shop-wide default and the rounding/inclusive settings change here.
    public async Task RefreshTaxSettingsAsync()
    {
        var taxSettings = await _shopContext.GetTaxAndRoundingSettingsAsync();
        var inventorySettings = await _shopContext.GetSettingsAsync();
        Cart.TaxRatePercent = taxSettings.DefaultTaxRatePercent;
        Cart.PricesIncludeTax = taxSettings.PricesIncludeTax;
        Cart.RoundingMode = taxSettings.RoundingMode;
        Cart.RoundingIncrement = taxSettings.RoundingIncrement;
        Cart.TrackInventory = inventorySettings?.TrackInventory ?? true;
        Cart.DefaultOutOfStockBehavior = inventorySettings?.OutOfStockBehavior ?? OutOfStockBehavior.Block;
    }

    private void OnProductSelected(Product product)
    {
        Cart.AddProduct(product);
    }

    private void ApplyOrderTypes(string? configuredTypes)
    {
        var previousSelection = SelectedOrderType;
        var types = (configuredTypes ?? "Sale")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        if (types.Count == 0) types.Add("Sale");

        OrderTypes.Clear();
        foreach (var type in types) OrderTypes.Add(type);
        SelectedOrderType = OrderTypes.FirstOrDefault(type =>
            string.Equals(type, previousSelection, StringComparison.OrdinalIgnoreCase)) ?? OrderTypes[0];
        OnPropertyChanged(nameof(HasMultipleOrderTypes));
    }

    [RelayCommand]
    private void OpenCheckout()
    {
        if (!Cart.HasItems) return;

        Checkout = new CheckoutViewModel(
            _saleService, Cart, CurrentCashierId, _paymentSettings, _salesBehaviorSettings, _customers,
            SelectedOrderType, _posFeedback, PosSoundsEnabled);
        Checkout.SaleCompleted += OnSaleCompleted;
        Checkout.Cancelled += OnCheckoutCancelled;
        IsCheckoutOpen = true;
    }

    [RelayCommand]
    private void OpenAdmin() => AdminRequested?.Invoke();

    [RelayCommand]
    private void HoldOrder() => TryHoldOrder();

    private bool TryHoldOrder()
    {
        if (!IsHeldOrdersEnabled || !Cart.HasItems) return false;

        var heldAt = DateTime.Now;
        var order = new HeldOrder(
            $"ORDER {heldAt:HH:mm} · {Cart.Lines.Sum(line => line.Quantity)} ITEMS",
            heldAt,
            Cart.Lines.Select(line => new HeldOrderLine(
                line.ProductId, line.Name, line.UnitPrice, line.UnitCost,
                line.TaxRatePercent, line.StockAvailable, line.Quantity, line.EnforceStock)).ToList(),
            Cart.DiscountAmount,
            Cart.AppliedVoucherCode,
            Cart.TotalAmount,
            SelectedOrderType);

        HeldOrders.Insert(0, order);
        try
        {
            _heldOrderStore.Save(HeldOrders);
        }
        catch (Exception ex)
        {
            HeldOrders.Remove(order);
            LastReceiptSummary = $"Could not save the held order: {ex.Message}";
            return false;
        }

        Cart.Clear();
        SetSuccessNotice($"Order held. {HeldOrders.Count} held order(s) ready to resume.");
        return true;
    }

    [RelayCommand]
    private async Task ResumeHeldOrder(HeldOrder? order)
    {
        if (!IsHeldOrdersEnabled || order is null || !HeldOrders.Contains(order)) return;

        if (Cart.HasItems)
        {
            if (!TryHoldOrder()) return;
        }

        HeldOrders.Remove(order);
        try
        {
            _heldOrderStore.Save(HeldOrders);
        }
        catch (Exception ex)
        {
            HeldOrders.Insert(0, order);
            LastReceiptSummary = $"Could not update held orders: {ex.Message}";
            return;
        }

        await Cart.RestoreHeldOrderAsync(order);
        SelectedOrderType = OrderTypes.FirstOrDefault(type =>
            string.Equals(type, order.OrderType, StringComparison.OrdinalIgnoreCase)) ?? OrderTypes[0];
        SetSuccessNotice($"Resumed {order.Name}. Stock is checked again at checkout.");
    }

    [RelayCommand]
    private void DiscardHeldOrder(HeldOrder? order)
    {
        if (!IsHeldOrdersEnabled || order is null) return;
        HeldOrders.Remove(order);
        try
        {
            _heldOrderStore.Save(HeldOrders);
        }
        catch (Exception ex)
        {
            HeldOrders.Insert(0, order);
            LastReceiptSummary = $"Could not discard the held order: {ex.Message}";
        }
    }

    private async void OnSaleCompleted(CompletedSaleResult result)
    {
        var completedCheckout = Checkout;
        if (PosSoundsEnabled)
            _posFeedback?.Play(PosSoundEvent.SaleCompleted);
        SetSuccessNotice($"Sale {result.SaleNumber} complete - total Rp {result.TotalAmount:N0}, change Rp {result.ChangeDue:N0}");

        var shouldPrint = _salesBehaviorSettings.AutoPrintReceiptAfterPayment && _receiptPrinter is not null && completedCheckout is not null;
        var lines = new List<SaleReceiptLine>();
        var taxBreakdown = new List<(decimal RatePercent, decimal Amount)>();
        if (shouldPrint)
        {
            lines = Cart.Lines.Select(l => new SaleReceiptLine(l.Name, l.Quantity, l.Subtotal)).ToList();

            // Recomputed here (before Cart.Clear() below) via the same
            // TaxCalculator SaleService just used, grouped by rate, so the
            // printed receipt can show a per-rate breakdown when a sale
            // mixed more than one tax rate, or a single "TAX (X%)" line
            // when it didn't - see SaleReceiptFormatter.Format.
            var taxCalculation = TaxCalculator.Calculate(
                Cart.Lines.Select(l => new TaxCalculator.LineInput(l.Subtotal, l.TaxRatePercent)).ToList(),
                Cart.DiscountAmount, Cart.PricesIncludeTax);

            taxBreakdown = taxCalculation.Lines
                .GroupBy(l => l.RatePercent)
                .Select(g => (RatePercent: g.Key, Amount: g.Sum(l => l.TaxAmount)))
                .OrderByDescending(g => g.RatePercent)
                .ToList();
        }

        CloseCheckout();
        Cart.Clear();
        var shouldOpenDrawer = EnableCashDrawerKick && _receiptPrinter?.SupportsCashDrawerKick == true &&
            completedCheckout?.PaymentLines.Any(line => line.IsCash) == true;
        _ = TryRunSaleDeviceActionsAsync(result, completedCheckout, shouldOpenDrawer, shouldPrint, lines, taxBreakdown);
        await ProductPicker.RefreshProductsAsync();
    }

    private async Task TryRunSaleDeviceActionsAsync(
        CompletedSaleResult result,
        CheckoutViewModel? checkout,
        bool openDrawer,
        bool printReceipt,
        IReadOnlyList<SaleReceiptLine> lines,
        IReadOnlyList<(decimal RatePercent, decimal Amount)> taxBreakdown)
    {
        if (openDrawer && _receiptPrinter is not null)
        {
            try
            {
                var drawerResult = await _receiptPrinter.OpenCashDrawerAsync();
                if (!drawerResult.Success)
                    LastReceiptSummary = $"Sale {result.SaleNumber} completed, but the cash drawer did not open: {drawerResult.ErrorMessage}";
            }
            catch (Exception ex)
            {
                LastReceiptSummary = $"Sale {result.SaleNumber} completed, but the cash drawer did not open: {ex.Message}";
            }
        }

        if (printReceipt && checkout is not null)
            await TryAutoPrintReceiptAsync(result, checkout, lines, taxBreakdown);
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
                checkout.OrderType,
                CurrentCashierName,
                lines,
                result.Subtotal,
                result.DiscountAmount,
                taxBreakdown,
                result.RoundingAdjustment,
                result.TotalAmount,
                checkout.PaymentLines.Select(payment => new SaleReceiptPayment(
                    payment.Method.ToString(), payment.Amount, payment.FeeAmount,
                    payment.ReferenceNumber, payment.CashTendered, payment.ChangeGiven)).ToList(),
                checkout.ChangeDue,
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

    private void SetSuccessNotice(string message) =>
        LastReceiptSummary = ShowPosNotifications ? message : string.Empty;

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
