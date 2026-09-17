using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Admin;

// Draft line used while picking quantities in the Refund dialog - mirrors
// PurchaseLineViewModel's role for the Purchases editor (a form-only
// wrapper, never persisted directly).
public partial class RefundLineFormItem : ViewModelBase
{
    public int SaleItemId { get; }
    public string ProductName { get; }
    public int OriginalQuantity { get; }
    public int AlreadyRefundedQuantity { get; }
    public int RefundableQuantity { get; }
    public decimal UnitPrice { get; }

    [ObservableProperty]
    private string _refundQuantity = "0";

    public RefundLineFormItem(
        int saleItemId, string productName, int originalQuantity,
        int alreadyRefundedQuantity, int refundableQuantity, decimal unitPrice)
    {
        SaleItemId = saleItemId;
        ProductName = productName;
        OriginalQuantity = originalQuantity;
        AlreadyRefundedQuantity = alreadyRefundedQuantity;
        RefundableQuantity = refundableQuantity;
        UnitPrice = unitPrice;
    }
}