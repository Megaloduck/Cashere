using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class SalesReportService : ISalesReportService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public SalesReportService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<SaleListItem>> GetSalesHistoryAsync(DateTime fromDate, DateTime toDate)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var (from, toExclusive) = NormalizeRange(fromDate, toDate);

        return await db.Sales
            .Where(s => s.SaleDate >= from && s.SaleDate < toExclusive)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleListItem(
                s.Id,
                s.SaleNumber,
                s.SaleDate,
                s.Cashier.DisplayName,
                s.Customer != null ? s.Customer.Name : null,
                s.Items.Count,
                s.TotalAmount,
                s.Status))
            .ToListAsync();
    }

    public async Task<SaleDetail?> GetSaleDetailAsync(int saleId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var sale = await db.Sales
            .Include(s => s.Cashier)
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Payments)
            .Include(s => s.Refunds).ThenInclude(r => r.ProcessedByCashier)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null) return null;

        return new SaleDetail(
            sale.Id,
            sale.SaleNumber,
            sale.SaleDate,
            sale.Cashier.DisplayName,
            sale.Customer?.Name,
            sale.Subtotal,
            sale.DiscountAmount,
            sale.TaxAmount,
            sale.TotalAmount,
            sale.Status,
            sale.Items
                .Select(i => new SaleLineDetail(i.Product.Name, i.Quantity, i.UnitPrice, i.UnitCostAtSale, i.Subtotal))
                .ToList(),
            sale.Payments
                .Select(p => new SalePaymentDetail(p.Method, p.Amount, p.ReferenceNumber))
                .ToList(),
            sale.Refunds
                .OrderByDescending(r => r.RefundDate)
                .Select(r => new RefundHistoryEntry(r.RefundDate, r.ProcessedByCashier.DisplayName, r.TotalAmount, r.IsVoid, r.Reason))
                .ToList());
    }

    public async Task<SalesReportSummary> GetSalesReportAsync(DateTime fromDate, DateTime toDate)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var (from, toExclusive) = NormalizeRange(fromDate, toDate);

        var sales = await db.Sales
            .Where(s => s.SaleDate >= from && s.SaleDate < toExclusive &&
                        (s.Status == SaleStatus.Completed || s.Status == SaleStatus.PartiallyRefunded))
            .Include(s => s.Items).ThenInclude(i => i.RefundLineItems)
            .AsNoTracking()
            .ToListAsync();

        var dailyBreakdown = sales
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var revenue = g.Sum(NetRevenue);
                var discount = g.Sum(s => s.DiscountAmount);
                var cost = g.Sum(NetCost);
                // Discount subtracted here now too, so the daily PROFIT
                // column matches the summary card above it instead of
                // silently running higher.
                return new DailySalesRow(g.Key, g.Count(), revenue, cost, revenue - discount - cost);
            })
            .ToList();

        var totalRevenue = sales.Sum(NetRevenue);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);
        var totalCost = sales.Sum(NetCost);

        return new SalesReportSummary(
            from,
            toExclusive.AddDays(-1),
            sales.Count,
            totalRevenue,
            totalDiscount,
            sales.Sum(s => s.TaxAmount),
            totalCost,
            totalRevenue - totalDiscount - totalCost,
            dailyBreakdown);
    }

    private static decimal NetRevenue(Sale sale) => sale.Items.Sum(i =>
        i.UnitPrice * (i.Quantity - i.RefundLineItems.Sum(l => l.Quantity)));

    private static decimal NetCost(Sale sale) => sale.Items.Sum(i =>
        i.UnitCostAtSale * (i.Quantity - i.RefundLineItems.Sum(l => l.Quantity)));

    private static (DateTime from, DateTime toExclusive) NormalizeRange(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var toExclusive = toDate.Date.AddDays(1);
        return (from, toExclusive);
    }
}