using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Models;

public class Shift
{
    public int Id { get; set; }
    public int CashierId { get; set; }
    public Cashier Cashier { get; set; } = null!;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public decimal StartingCash { get; set; }
    // Both null while the shift is open - populated together at close time.
    public decimal? ExpectedCashAtClose { get; set; }
    public decimal? ActualCashAtClose { get; set; }
    public decimal? DiscrepancyAmount { get; set; }
    public string? Notes { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
}