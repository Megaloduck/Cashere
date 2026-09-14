using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record ShiftListItem(
    int Id,
    string CashierName,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal StartingCash,
    decimal? ExpectedCashAtClose,
    decimal? ActualCashAtClose,
    decimal? DiscrepancyAmount,
    ShiftStatus Status);

// Cash-drawer reconciliation for a single till - mirrors ActiveCartService's
// "one till is enough for MVP" assumption: only one shift can be open at a
// time regardless of which cashier opened it, rather than tracking
// concurrent shifts per cashier.
public interface IShiftAdminService
{
    Task<Shift?> GetOpenShiftAsync();
    Task<decimal> GetExpectedCashAsync(int shiftId);
    Task<Shift> OpenShiftAsync(int cashierId, decimal startingCash, string? notes);
    Task<Shift> CloseShiftAsync(int shiftId, decimal actualCash, string? notes);
    Task<List<ShiftListItem>> GetRecentShiftsAsync(int take = 20);
}