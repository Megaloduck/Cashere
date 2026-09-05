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

    public event Action<CompletedSaleResult>? SaleCompleted;
    public event Action? Cancelled;

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    [ObservableProperty]
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private decimal _amountTendered;

    [ObservableProperty]
    private string? _referenceNumber;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isProcessing;

    public decimal TotalDue => _cart.TotalAmount;

    public decimal ChangeDue => PaymentMethod == PaymentMethod.Cash
        ? Math.Max(0, AmountTendered - TotalDue)
        : 0;

    public bool IsCashPayment => PaymentMethod == PaymentMethod.Cash;
    public bool IsNonCashPayment => !IsCashPayment;

    public bool CanComplete =>
        !IsProcessing &&
        _cart.HasItems &&
        (!IsCashPayment || AmountTendered >= TotalDue);

    public CheckoutViewModel(ISaleService saleService, CartViewModel cart, int cashierId)
    {
        _saleService = saleService;
        _cart = cart;
        _cashierId = cashierId;
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
        OnPropertyChanged(nameof(CanComplete));

        if (value != PaymentMethod.Cash)
        {
            AmountTendered = TotalDue;
        }
    }

    partial void OnIsProcessingChanged(bool value) => OnPropertyChanged(nameof(CanComplete));

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
