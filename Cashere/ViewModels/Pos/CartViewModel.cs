using System;
using System.Collections.ObjectModel;
using System.Linq;
using Cashere.Models;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Cashere.Services;

namespace Cashere.ViewModels.Pos;

public partial class CartViewModel : ViewModelBase
{
    private readonly IVoucherAdminService? _voucherAdmin;

    // Kept only while a voucher is applied, so a subtotal change (item
    // added/removed/quantity edited) can recompute a percentage voucher's Rp
    // value against the new subtotal - RaiseTotalsChanged() below is the one
    // place every subtotal-affecting mutation already funnels through.
    private VoucherDiscountType? _appliedVoucherType;
    private decimal _appliedVoucherValue;

    public ObservableCollection<CartLineViewModel> Lines { get; } = new();

    // The shop-wide default rate (Settings -> Business Info -> Tax Rates) -
    // used to resolve a new line's rate in AddProduct when its product has
    // no category, or its category has no TaxRate assigned. Already-added
    // lines keep whatever rate they resolved to at add-time; see
    // CartLineViewModel.TaxRatePercent.
    [ObservableProperty]
    private decimal _taxRatePercent;

    // Settings -> Business Info -> "Prices include tax". See TaxCalculator.
    [ObservableProperty]
    private bool _pricesIncludeTax;

    // Settings -> Business Info -> Rounding. See TaxCalculator.ApplyRounding.
    [ObservableProperty]
    private RoundingMode _roundingMode = RoundingMode.None;

    [ObservableProperty]
    private decimal _roundingIncrement;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private string _voucherCodeInput = string.Empty;

    [ObservableProperty]
    private string? _appliedVoucherCode;

    [ObservableProperty]
    private string? _voucherErrorMessage;

    [ObservableProperty]
    private bool _isApplyingVoucher;

    public decimal Subtotal => Lines.Sum(l => l.Subtotal);

    // Computed by the same TaxCalculator function SaleService uses at sale
    // completion, so this preview can never disagree with what actually
    // gets charged.
    public decimal TaxAmount => TaxCalculator.Calculate(
        Lines.Select(l => new TaxCalculator.LineInput(l.Subtotal, l.TaxRatePercent)).ToList(),
        DiscountAmount, PricesIncludeTax).TotalTax;

    private decimal PreRoundingTotal => PricesIncludeTax
        ? Subtotal - DiscountAmount
        : Subtotal - DiscountAmount + TaxAmount;

    public decimal TotalAmount => TaxCalculator.ApplyRounding(PreRoundingTotal, RoundingMode, RoundingIncrement);

    // How much rounding changed the total by - shown next to TOTAL in the
    // cart only when HasRoundingAdjustment is true (see CartView.axaml).
    public decimal RoundingAdjustment => TotalAmount - PreRoundingTotal;
    public bool HasRoundingAdjustment => RoundingAdjustment != 0;

    public bool HasItems => Lines.Count > 0;

    public CartViewModel(IVoucherAdminService? voucherAdmin = null)
    {
        _voucherAdmin = voucherAdmin;
    }

    public void AddProduct(Product product)
    {
        var existing = Lines.FirstOrDefault(l => l.ProductId == product.Id);
        if (existing is not null)
        {
            if (existing.Quantity < existing.StockAvailable)
            {
                existing.Quantity++;
            }
            return;
        }

        if (product.StockQuantity <= 0)
        {
            return;
        }

        var taxRatePercent = TaxCalculator.ResolveRatePercent(product.Category, TaxRatePercent);

        Lines.Add(new CartLineViewModel(
            product.Id,
            product.Name,
            product.SellingPrice,
            product.CostPrice,
            taxRatePercent,
            product.StockQuantity,
            quantity: 1,
            onChanged: RaiseTotalsChanged));

        RaiseTotalsChanged();
    }

    // Public on purpose (unlike the other mutators below) so PosViewModel can
    // reset the cart after a completed sale without going through a command.
    public void Clear()
    {
        Lines.Clear();
        _appliedVoucherType = null;
        _appliedVoucherValue = 0;
        AppliedVoucherCode = null;
        VoucherCodeInput = string.Empty;
        VoucherErrorMessage = null;
        DiscountAmount = 0;
        RaiseTotalsChanged();
    }

    [RelayCommand]
    private void RemoveLine(CartLineViewModel? line)
    {
        if (line is null) return;
        Lines.Remove(line);
        RaiseTotalsChanged();
    }

    [RelayCommand]
    private void ClearCart() => Clear();

    [RelayCommand]
    private async Task ApplyVoucher()
    {
        VoucherErrorMessage = null;
        if (_voucherAdmin is null || string.IsNullOrWhiteSpace(VoucherCodeInput)) return;

        IsApplyingVoucher = true;
        try
        {
            var result = await _voucherAdmin.ValidateVoucherAsync(VoucherCodeInput.Trim(), Subtotal);
            if (!result.IsValid)
            {
                VoucherErrorMessage = result.ErrorMessage ?? "Voucher code is not valid.";
                return;
            }

            AppliedVoucherCode = VoucherCodeInput.Trim().ToUpperInvariant();
            _appliedVoucherType = result.DiscountType;
            _appliedVoucherValue = result.DiscountValue;
            DiscountAmount = result.DiscountAmount;
            VoucherCodeInput = string.Empty;
        }
        finally
        {
            IsApplyingVoucher = false;
        }
    }

    [RelayCommand]
    private void RemoveVoucher()
    {
        AppliedVoucherCode = null;
        _appliedVoucherType = null;
        _appliedVoucherValue = 0;
        DiscountAmount = 0;
        VoucherErrorMessage = null;
    }

    partial void OnDiscountAmountChanged(decimal value) => RaiseTotalsChanged();
    partial void OnTaxRatePercentChanged(decimal value) => RaiseTotalsChanged();
    partial void OnPricesIncludeTaxChanged(bool value) => RaiseTotalsChanged();
    partial void OnRoundingModeChanged(RoundingMode value) => RaiseTotalsChanged();
    partial void OnRoundingIncrementChanged(decimal value) => RaiseTotalsChanged();

    private void RaiseTotalsChanged()
    {
        // Keeps a percentage voucher's Rp discount tracking the live
        // subtotal, and re-clamps a fixed voucher so it can never discount
        // more than what's actually in the cart. DiscountAmount's own setter
        // no-ops if the recomputed value is unchanged, so this can't loop.
        if (_appliedVoucherType == VoucherDiscountType.Percentage)
        {
            DiscountAmount = Math.Round(Subtotal * (_appliedVoucherValue / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else if (_appliedVoucherType == VoucherDiscountType.FixedAmount)
        {
            DiscountAmount = Math.Min(_appliedVoucherValue, Subtotal);
        }

        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(RoundingAdjustment));
        OnPropertyChanged(nameof(HasRoundingAdjustment));
        OnPropertyChanged(nameof(HasItems));
    }
}
