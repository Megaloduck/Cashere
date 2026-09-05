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

    [ObservableProperty]
    private int _quantity;

    public decimal Subtotal => UnitPrice * Quantity;

    public CartLineViewModel(
        int productId,
        string name,
        decimal unitPrice,
        decimal unitCost,
        int stockAvailable,
        int quantity,
        Action onChanged)
    {
        ProductId = productId;
        Name = name;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
        StockAvailable = stockAvailable;
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
        if (Quantity < StockAvailable)
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