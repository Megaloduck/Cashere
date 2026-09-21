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
    // Routed through ClockPreferenceService so this agrees with the header
    // clock and every other timestamp on screen, instead of showing the
    // raw stored UTC hours directly.
    public string DurationDisplay
    {
        get
        {
            var opened = ClockPreferenceService.ToDisplay(OpenedAt);
            return ClosedAt is { } closedAt
                ? $"{opened:HH:mm} - {ClockPreferenceService.ToDisplay(closedAt):HH:mm}"
                : $"{opened:HH:mm} - ...";
        }
    }
}
public interface IShiftAdminService
{
    Task<Shift?> GetOpenShiftAsync();
    Task<decimal> GetExpectedCashAsync(int shiftId);
    Task<Shift> OpenShiftAsync(int cashierId, decimal startingCash, string? notes);
    Task<Shift> CloseShiftAsync(int shiftId, decimal actualCash, string? notes);
    Task<List<ShiftListItem>> GetRecentShiftsAsync(int take = 20);
}