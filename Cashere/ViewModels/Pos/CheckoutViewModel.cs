using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Pos;

public partial class CheckoutViewModel : ViewModelBase
{
    private readonly ISaleService _saleService;
    private readonly CartViewModel _cart;
    private readonly int _cashierId;
    private readonly PaymentSettings _paymentSettings;

    public event Action<CompletedSaleResult>? SaleCompleted;
    public event Action? Cancelled;

    // Filtered to whatever Settings -> Payments currently has enabled -
    // SaleService independently re-checks this at completion as a backstop.
    public IReadOnlyList<PaymentMethod> PaymentMethods { get; }

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    [ObservableProperty]
    private decimal _amountTendered;

    [ObservableProperty]
    private string? _referenceNumber;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _isPaymentConfirmed;

    public decimal TotalDue => _cart.TotalAmount;

    public decimal ChangeDue => PaymentMethod == PaymentMethod.Cash
        ? Math.Max(0, AmountTendered - TotalDue)
        : 0;

    public bool IsCashPayment => PaymentMethod == PaymentMethod.Cash;
    public bool IsNonCashPayment => !IsCashPayment;

    // Settings -> Payments -> account info for whichever method is currently
    // selected, shown as a cashier hint (e.g. which QRIS merchant ID or EDC
    // terminal to expect the payment on). Only meaningful when non-cash.
    public string? SelectedMethodAccountInfo => _paymentSettings.AccountInfoFor(PaymentMethod);
    public bool HasSelectedMethodAccountInfo => !string.IsNullOrWhiteSpace(SelectedMethodAccountInfo);

    public bool IsConfirmationRequired => _paymentSettings.RequireConfirmationForNonCash && IsNonCashPayment;

    public bool CanComplete =>
        !IsProcessing &&
        _cart.HasItems &&
        (!IsCashPayment || AmountTendered >= TotalDue) &&
        (!IsConfirmationRequired || IsPaymentConfirmed);

    public CheckoutViewModel(ISaleService saleService, CartViewModel cart, int cashierId, PaymentSettings paymentSettings)
    {
        _saleService = saleService;
        _cart = cart;
        _cashierId = cashierId;
        _paymentSettings = paymentSettings;

        PaymentMethods = Enum.GetValues<PaymentMethod>()
            .Where(m => paymentSettings.IsMethodEnabled(m))
            .ToList();

        // Falls back to whatever's first enabled if Cash itself got disabled -
        // PaymentSettingsViewModel.Save() already blocks disabling every
        // method, so PaymentMethods is guaranteed non-empty here.
        _paymentMethod = PaymentMethods.Contains(PaymentMethod.Cash)
            ? PaymentMethod.Cash
            : PaymentMethods[0];

        AmountTendered = TotalDue;
    }

    partial void OnAmountTenderedChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(CanComplete));
    }

    partial void OnPaymentMethodChanged(PaymentMethod value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(IsNonCashPayment));
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(SelectedMethodAccountInfo));
        OnPropertyChanged(nameof(HasSelectedMethodAccountInfo));
        OnPropertyChanged(nameof(IsConfirmationRequired));
        OnPropertyChanged(nameof(CanComplete));

        // A confirmation ticked for one method shouldn't silently carry over
        // if the cashier switches to a different method mid-checkout.
        IsPaymentConfirmed = false;

        if (value != PaymentMethod.Cash)
        {
            AmountTendered = TotalDue;
        }
    }

    partial void OnIsProcessingChanged(bool value) => OnPropertyChanged(nameof(CanComplete));
    partial void OnIsPaymentConfirmedChanged(bool value) => OnPropertyChanged(nameof(CanComplete));

    [RelayCommand]
    private async Task CompleteAsync()
    {
        ErrorMessage = null;
        IsProcessing = true;
        try
        {
            var request = new CompleteSaleRequest(
                CashierId: _cashierId,
                CustomerId: null,
                Lines: _cart.Lines.Select(l => new SaleLineRequest(l.ProductId, l.Quantity, l.UnitPrice)).ToList(),
                DiscountAmount: _cart.DiscountAmount,
                TaxRatePercent: _cart.TaxRatePercent,
                PaymentMethod: this.PaymentMethod,
                AmountTendered: IsCashPayment ? AmountTendered : TotalDue,
                PaymentReferenceNumber: IsCashPayment ? null : ReferenceNumber);

            var result = await _saleService.CompleteSaleAsync(request);
            SaleCompleted?.Invoke(result);
        }
        catch (InsufficientStockException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Checkout failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke();
    }
}