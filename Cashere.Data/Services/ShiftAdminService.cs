using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class ShiftAdminService : IShiftAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public ShiftAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Shift?> GetOpenShiftAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Shifts
            .Include(s => s.Cashier)
            .Where(s => s.Status == ShiftStatus.Open)
            .OrderByDescending(s => s.OpenedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<decimal> GetExpectedCashAsync(int shiftId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var shift = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId)
            ?? throw new AdminValidationException($"Shift {shiftId} was not found.");

        // Voided/refunded sales excluded, same philosophy as
        // SalesReportService - they never counted as real income, so they
        // shouldn't count toward what's expected in the drawer either.
        var cashDuringShift = await db.Payments
            .Where(p => p.Method == PaymentMethod.Cash
                        && p.PaidAt >= shift.OpenedAt
                        && (shift.ClosedAt == null || p.PaidAt <= shift.ClosedAt)
                        && p.Sale.Status == SaleStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return shift.StartingCash + cashDuringShift;
    }

    public async Task<Shift> OpenShiftAsync(int cashierId, decimal startingCash, string? notes)
    {
        if (startingCash < 0)
        {
            throw new AdminValidationException("Starting cash can't be negative.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var alreadyOpen = await db.Shifts.AnyAsync(s => s.Status == ShiftStatus.Open);
        if (alreadyOpen)
        {
            throw new AdminValidationException("A shift is already open - close it before opening a new one.");
        }

        var shift = new Shift
        {
            CashierId = cashierId,
            OpenedAt = DateTime.UtcNow,
            StartingCash = startingCash,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = ShiftStatus.Open
        };

        db.Shifts.Add(shift);
        await db.SaveChangesAsync();
        return shift;
    }

    public async Task<Shift> CloseShiftAsync(int shiftId, decimal actualCash, string? notes)
    {
        if (actualCash < 0)
        {
            throw new AdminValidationException("Actual cash can't be negative.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId)
            ?? throw new AdminValidationException($"Shift {shiftId} was not found.");

        if (shift.Status == ShiftStatus.Closed)
        {
            throw new AdminValidationException("This shift is already closed.");
        }

        var expectedCash = await GetExpectedCashAsync(shiftId);

        shift.ClosedAt = DateTime.UtcNow;
        shift.ExpectedCashAtClose = expectedCash;
        shift.ActualCashAtClose = actualCash;
        shift.DiscrepancyAmount = actualCash - expectedCash;
        shift.Status = ShiftStatus.Closed;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            // Append rather than overwrite - the opening note (if any) still
            // matters as context for why starting cash was what it was.
            shift.Notes = string.IsNullOrWhiteSpace(shift.Notes)
                ? notes.Trim()
                : $"{shift.Notes}\n---\nClose: {notes.Trim()}";
        }

        await db.SaveChangesAsync();
        return shift;
    }

    public async Task<List<ShiftListItem>> GetRecentShiftsAsync(int take = 20)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        return await db.Shifts
            .OrderByDescending(s => s.OpenedAt)
            .Take(take)
            .Select(s => new ShiftListItem(
                s.Id,
                s.Cashier.DisplayName,
                s.OpenedAt,
                s.ClosedAt,
                s.StartingCash,
                s.ExpectedCashAtClose,
                s.ActualCashAtClose,
                s.DiscrepancyAmount,
                s.Status))
            .AsNoTracking()
            .ToListAsync();
    }
}