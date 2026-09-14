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

using System;
using System.Collections.Generic;
using System.Linq;
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

    public event Action<CompletedSaleResult>? SaleCompleted;
    public event Action? Cancelled;

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; }
    public IReadOnlyList<Customer> Customers { get; }

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    [ObservableProperty]
    private Customer? _selectedCustomer;

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

    public string? SelectedMethodAccountInfo => _paymentSettings.AccountInfoFor(PaymentMethod);
    public bool HasSelectedMethodAccountInfo => !string.IsNullOrWhiteSpace(SelectedMethodAccountInfo);

    public bool IsConfirmationRequired => _paymentSettings.RequireConfirmationForNonCash && IsNonCashPayment;

    // Settings -> Sales Behavior. When on, Complete Sale stays disabled
    // until a customer is picked - SaleService independently re-checks this
    // at completion as a backstop, same pattern as payment methods.
    public bool IsCustomerRequired => _salesBehaviorSettings.RequireCustomerBeforeCheckout;

    public bool CanComplete =>
        !IsProcessing &&
        _cart.HasItems &&
        (!IsCashPayment || AmountTendered >= TotalDue) &&
        (!IsConfirmationRequired || IsPaymentConfirmed) &&
        (!IsCustomerRequired || SelectedCustomer is not null);

    public CheckoutViewModel(
        ISaleService saleService,
        CartViewModel cart,
        int cashierId,
        PaymentSettings paymentSettings,
        SalesBehaviorSettings salesBehaviorSettings,
        IReadOnlyList<Customer> customers)
    {
        _saleService = saleService;
        _cart = cart;
        _cashierId = cashierId;
        _paymentSettings = paymentSettings;
        _salesBehaviorSettings = salesBehaviorSettings;
        Customers = customers;

        PaymentMethods = Enum.GetValues<PaymentMethod>()
            .Where(m => paymentSettings.IsMethodEnabled(m))
            .ToList();

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

    partial void OnSelectedCustomerChanged(Customer? value) => OnPropertyChanged(nameof(CanComplete));

    partial void OnPaymentMethodChanged(PaymentMethod value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(IsNonCashPayment));
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(SelectedMethodAccountInfo));
        OnPropertyChanged(nameof(HasSelectedMethodAccountInfo));
        OnPropertyChanged(nameof(IsConfirmationRequired));
        OnPropertyChanged(nameof(CanComplete));

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
                CustomerId: SelectedCustomer?.Id,
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