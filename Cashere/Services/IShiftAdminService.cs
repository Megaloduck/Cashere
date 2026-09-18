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
    ShiftStatus Status)
{
    // EXAMPLE = "09:00 - 17:30" for a closed shift, "09:00 - ..." while still open.
    public string DurationDisplay => ClosedAt is { } closedAt
        ? $"{OpenedAt:HH:mm} - {closedAt:HH:mm}"
        : $"{OpenedAt:HH:mm} - ...";
}
public interface IShiftAdminService
{
    Task<Shift?> GetOpenShiftAsync();
    Task<decimal> GetExpectedCashAsync(int shiftId);
    Task<Shift> OpenShiftAsync(int cashierId, decimal startingCash, string? notes);
    Task<Shift> CloseShiftAsync(int shiftId, decimal actualCash, string? notes);
    Task<List<ShiftListItem>> GetRecentShiftsAsync(int take = 20);
}