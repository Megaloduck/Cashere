using System;
using System.Collections.ObjectModel;
using System.Linq;
using Cashere.Models;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Pos;

public partial class CartViewModel : ViewModelBase
{
    public ObservableCollection<CartLineViewModel> Lines { get; } = new();

    [ObservableProperty]
    private decimal _taxRatePercent;

    [ObservableProperty]
    private decimal _discountAmount;

    public decimal Subtotal => Lines.Sum(l => l.Subtotal);

    public decimal TaxAmount => Math.Round(
        (Subtotal - DiscountAmount) * (TaxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);

    public decimal TotalAmount => Subtotal - DiscountAmount + TaxAmount;

    public bool HasItems => Lines.Count > 0;

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

        Lines.Add(new CartLineViewModel(
            product.Id,
            product.Name,
            product.SellingPrice,
            product.CostPrice,
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

    partial void OnDiscountAmountChanged(decimal value) => RaiseTotalsChanged();
    partial void OnTaxRatePercentChanged(decimal value) => RaiseTotalsChanged();

    private void RaiseTotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(HasItems));
    }
}
