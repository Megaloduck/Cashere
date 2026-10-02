using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly SalesBehaviorSettings _salesBehaviorSettings;
    private readonly string _orderType;
    private readonly IPosFeedbackService? _posFeedback;
    private readonly bool _posSoundsEnabled;

    public event Action<CompletedSaleResult>? SaleCompleted;
    public event Action? Cancelled;

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; }
    public IReadOnlyList<Customer> Customers { get; }

    // Every payment line committed so far via AddPaymentLineCommand. Usually
    // just one (the common "pay it all in one method" case), but a cashier
    // can add several to split a sale across methods - see AddPaymentLine.
    public ObservableCollection<PaymentLineViewModel> PaymentLines { get; } = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _isPaymentConfirmed;

    // ---- The pending line being composed (not yet added) --------------------

    [ObservableProperty]
    private PaymentMethod _pendingMethod;

    // Non-cash: how much of the remaining balance this line settles.
    // Cash: ignored in favor of PendingCashTendered below.
    [ObservableProperty]
    private decimal _pendingAmount;

    // Cash only: what's physically being handed over - may exceed the
    // remaining balance, in which case the excess becomes change the moment
    // the line is added (see AddPaymentLine).
    [ObservableProperty]
    private decimal _pendingCashTendered;

    [ObservableProperty]
    private string? _pendingReferenceNumber;

    // TotalDue already reflects Settings -> Business Info -> Rounding - see
    // CartViewModel.TotalAmount / TaxCalculator.ApplyRounding.
    public decimal TotalDue => _cart.TotalAmount;

    public decimal AmountPaid => PaymentLines.Sum(l => l.Amount);
    public decimal AmountRemaining => Math.Max(0, TotalDue - AmountPaid);
    public bool IsFullyPaid => AmountRemaining <= 0;
    public bool HasPaymentLines => PaymentLines.Count > 0;

    public decimal TotalFees => PaymentLines.Sum(l => l.FeeAmount);
    public decimal TotalChangeGiven => PaymentLines.Sum(l => l.ChangeGiven);
    public decimal CashTendered => PaymentLines.Where(l => l.IsCash).Sum(l => l.CashTendered);
    public decimal ChangeDue => PaymentLines.Sum(l => l.ChangeGiven);

    public bool IsPendingCash => PendingMethod == PaymentMethod.Cash;

    // What this pending line would actually settle if added right now -
    // capped at the remaining balance, since a payment line can never
    // over-apply (a cash overpayment becomes change, not extra "paid").
    private decimal PendingEffectiveAmount => IsPendingCash
        ? Math.Min(PendingCashTendered, AmountRemaining)
        : Math.Min(PendingAmount, AmountRemaining);

    public decimal PendingFeeAmount => _paymentSettings.ComputeFee(PendingMethod, PendingEffectiveAmount);
    public bool HasPendingFee => PendingFeeAmount > 0;
    public decimal PendingChangeDue => IsPendingCash ? Math.Max(0, PendingCashTendered - AmountRemaining) : 0;
    public bool HasPendingChangeDue => PendingChangeDue > 0;

    public string? PendingMethodAccountInfo => _paymentSettings.AccountInfoFor(PendingMethod);
    public bool HasPendingMethodAccountInfo => !string.IsNullOrWhiteSpace(PendingMethodAccountInfo);

    public bool IsPendingConfirmationRequired =>
        _paymentSettings.RequireConfirmationForNonCash && PendingMethod != PaymentMethod.Cash;

    public bool CanAddPaymentLine =>
        !IsProcessing &&
        AmountRemaining > 0 &&
        PendingEffectiveAmount > 0 &&
        (!IsPendingConfirmationRequired || IsPaymentConfirmed);

    public bool IsCustomerRequired => _salesBehaviorSettings.RequireCustomerBeforeCheckout;
    public string OrderType => _orderType;

    public bool CanComplete =>
        !IsProcessing &&
        _cart.HasItems &&
        PaymentLines.Count > 0 &&
        IsFullyPaid &&
        (!IsCustomerRequired || SelectedCustomer is not null);

    public CheckoutViewModel(
        ISaleService saleService,
        CartViewModel cart,
        int cashierId,
        PaymentSettings paymentSettings,
        SalesBehaviorSettings salesBehaviorSettings,
        IReadOnlyList<Customer> customers,
        string orderType = "Sale",
        IPosFeedbackService? posFeedback = null,
        bool posSoundsEnabled = false)
    {
        _saleService = saleService;
        _cart = cart;
        _cashierId = cashierId;
        _paymentSettings = paymentSettings;
        _salesBehaviorSettings = salesBehaviorSettings;
        _orderType = orderType;
        _posFeedback = posFeedback;
        _posSoundsEnabled = posSoundsEnabled;
        Customers = customers;

        PaymentMethods = Enum.GetValues<PaymentMethod>()
            .Where(m => paymentSettings.IsMethodEnabled(m))
            .ToList();

        _pendingMethod = PaymentMethods.Contains(PaymentMethod.Cash)
            ? PaymentMethod.Cash
            : PaymentMethods[0];

        ResetPendingLine();
    }

    private void ResetPendingLine()
    {
        PendingAmount = AmountRemaining;
        PendingCashTendered = AmountRemaining;
        PendingReferenceNumber = null;
        IsPaymentConfirmed = false;
        RaisePendingChanged();
    }

    private void RaisePendingChanged()
    {
        OnPropertyChanged(nameof(IsPendingCash));
        OnPropertyChanged(nameof(PendingFeeAmount));
        OnPropertyChanged(nameof(HasPendingFee));
        OnPropertyChanged(nameof(PendingChangeDue));
        OnPropertyChanged(nameof(HasPendingChangeDue));
        OnPropertyChanged(nameof(PendingMethodAccountInfo));
        OnPropertyChanged(nameof(HasPendingMethodAccountInfo));
        OnPropertyChanged(nameof(IsPendingConfirmationRequired));
        OnPropertyChanged(nameof(CanAddPaymentLine));
    }

    private void RaiseTotalsChanged()
    {
        OnPropertyChanged(nameof(AmountPaid));
        OnPropertyChanged(nameof(AmountRemaining));
        OnPropertyChanged(nameof(IsFullyPaid));
        OnPropertyChanged(nameof(TotalFees));
        OnPropertyChanged(nameof(TotalChangeGiven));
        OnPropertyChanged(nameof(CanComplete));
        RaisePendingChanged();
    }

    partial void OnPendingMethodChanged(PaymentMethod value)
    {
        IsPaymentConfirmed = false;
        RaisePendingChanged();
    }

    partial void OnPendingAmountChanged(decimal value) => RaisePendingChanged();
    partial void OnPendingCashTenderedChanged(decimal value) => RaisePendingChanged();
    partial void OnIsPaymentConfirmedChanged(bool value) => RaisePendingChanged();
    partial void OnIsProcessingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanComplete));
        RaisePendingChanged();
    }
    partial void OnSelectedCustomerChanged(Customer? value) => OnPropertyChanged(nameof(CanComplete));

    [RelayCommand]
    private void AddPaymentLine()
    {
        if (!CanAddPaymentLine) return;

        var effectiveAmount = PendingEffectiveAmount;
        var feeAmount = PendingFeeAmount;

        var line = IsPendingCash
            ? new PaymentLineViewModel(
                PaymentMethod.Cash, effectiveAmount, PendingCashTendered, PendingChangeDue, feeAmount, null)
            : new PaymentLineViewModel(
                PendingMethod, effectiveAmount, 0, 0, feeAmount,
                string.IsNullOrWhiteSpace(PendingReferenceNumber) ? null : PendingReferenceNumber.Trim());

        PaymentLines.Add(line);
        ResetPendingLine();
        RaiseTotalsChanged();
    }

    [RelayCommand]
    private void RemovePaymentLine(PaymentLineViewModel? line)
    {
        if (line is null) return;
        PaymentLines.Remove(line);
        ResetPendingLine();
        RaiseTotalsChanged();
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        ErrorMessage = null;
        IsProcessing = true;
        try
        {
            var request = new CompleteSaleRequest(
                CashierId: _cashierId,
                CustomerId: SelectedCustomer?.Id,
                Lines: _cart.Lines
                    .Select(l => new SaleLineRequest(l.ProductId, l.Quantity, l.UnitPrice, l.TaxRatePercent))
                    .ToList(),
                DiscountAmount: _cart.DiscountAmount,
                Payments: PaymentLines
                    .Select(l => new PaymentInput(l.Method, l.Amount, l.FeeAmount, l.ReferenceNumber))
                    .ToList(),
                AmountTendered: PaymentLines.Where(l => l.IsCash).Sum(l => l.CashTendered),
                VoucherCode: _cart.AppliedVoucherCode,
                OrderType: _orderType);

            var result = await _saleService.CompleteSaleAsync(request);
            SaleCompleted?.Invoke(result);
        }
        catch (InsufficientStockException ex)
        {
            ErrorMessage = ex.Message;
            PlayCheckoutError();
        }
        catch (InvalidVoucherException ex)
        {
            ErrorMessage = ex.Message;
            PlayCheckoutError();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Checkout failed: {ex.Message}";
            PlayCheckoutError();
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void PlayCheckoutError()
    {
        if (_posSoundsEnabled)
            _posFeedback?.Play(PosSoundEvent.CheckoutFailed);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke();
    }
}
