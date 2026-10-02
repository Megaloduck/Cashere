using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.ViewModels.Pos;

// One committed line in a (possibly split) payment. Once added it's locked
// in - CheckoutViewModel.RemovePaymentLineCommand is the only way to undo
// it. Mirrors CartLineViewModel's role: a small, mostly-immutable record of
// what the cashier decided, not a live-editable form.
public class PaymentLineViewModel : ViewModelBase
{
    public PaymentMethod Method { get; }

    // How much of the sale total this line settles - never more than the
    // balance that was remaining at the moment it was added.
    public decimal Amount { get; }

    // Cash only: what was physically handed over for this line. Zero for
    // every other method. ChangeGiven is derived from it at add-time and
    // frozen here so removing a later line can't retroactively change it.
    public decimal CashTendered { get; }
    public decimal ChangeGiven { get; }

    // Surcharge for using this method (Settings -> Payments -> fee %).
    // Informational - never counts toward Amount or the sale balance.
    public decimal FeeAmount { get; }

    public string? ReferenceNumber { get; }

    public bool IsCash => Method == PaymentMethod.Cash;
    public bool HasFee => FeeAmount > 0;
    public bool HasChange => ChangeGiven > 0;
    public bool HasReference => !string.IsNullOrWhiteSpace(ReferenceNumber);

    public PaymentLineViewModel(
        PaymentMethod method,
        decimal amount,
        decimal cashTendered,
        decimal changeGiven,
        decimal feeAmount,
        string? referenceNumber)
    {
        Method = method;
        Amount = amount;
        CashTendered = cashTendered;
        ChangeGiven = changeGiven;
        FeeAmount = feeAmount;
        ReferenceNumber = referenceNumber;
    }
}
