using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Pos;

public partial class CartLineViewModel : ViewModelBase
{
    private readonly Action _onChanged;

    public int ProductId { get; }
    public string Name { get; }
    public decimal UnitPrice { get; }
    public decimal UnitCost { get; }
    public int StockAvailable { get; }
    public bool EnforceStock { get; }

    // Resolved once, at the moment this line was added to the cart (see
    // CartViewModel.AddProduct / TaxCalculator.ResolveRatePercent) - a later
    // change to the product's category rate never rewrites a line already
    // sitting in an open cart, same "snapshot at add-time" spirit as
    // UnitPrice/UnitCost above.
    public decimal TaxRatePercent { get; }

    [ObservableProperty]
    private int _quantity;

    public decimal Subtotal => UnitPrice * Quantity;

    public CartLineViewModel(
        int productId,
        string name,
        decimal unitPrice,
        decimal unitCost,
        decimal taxRatePercent,
        int stockAvailable,
        int quantity,
        Action onChanged,
        bool enforceStock = true)
    {
        ProductId = productId;
        Name = name;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
        TaxRatePercent = taxRatePercent;
        StockAvailable = stockAvailable;
        EnforceStock = enforceStock;
        _quantity = quantity;
        _onChanged = onChanged;
    }

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Subtotal));
        _onChanged();
    }

    [RelayCommand]
    private void Increment()
    {
        if (!EnforceStock || Quantity < StockAvailable)
        {
            Quantity++;
        }
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Quantity > 1)
        {
            Quantity--;
        }
    }
}
