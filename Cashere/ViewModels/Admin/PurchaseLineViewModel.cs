using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Admin;

// Draft line used while assembling a new purchase in the editor - only turned
// into a real PurchaseItem once Save() succeeds server-side.
public partial class PurchaseLineViewModel : ViewModelBase
{
    public int ProductId { get; }
    public string ProductName { get; }

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _unitCost;

    public decimal Subtotal => Quantity * UnitCost;

    public PurchaseLineViewModel(int productId, string productName, int quantity, decimal unitCost)
    {
        ProductId = productId;
        ProductName = productName;
        _quantity = quantity;
        _unitCost = unitCost;
    }

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(Subtotal));
    partial void OnUnitCostChanged(decimal value) => OnPropertyChanged(nameof(Subtotal));
}