using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record SaleListItem(
    int Id,
    string SaleNumber,
    DateTime SaleDate,
    string CashierName,
    string? CustomerName,
    int ItemCount,
    decimal TotalAmount,
    SaleStatus Status);

public record SaleLineDetail(string ProductName, int Quantity, decimal UnitPrice, decimal UnitCostAtSale, decimal Subtotal);

public record SalePaymentDetail(PaymentMethod Method, decimal Amount, string? ReferenceNumber);

public record SaleDetail(
    int Id,
    string SaleNumber,
    DateTime SaleDate,
    string CashierName,
    string? CustomerName,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    SaleStatus Status,
    IReadOnlyList<SaleLineDetail> Lines,
    IReadOnlyList<SalePaymentDetail> Payments)
{
    // UnitCostAtSale is exactly what SaleService froze at transaction time -
    // this is the whole reason that column exists, so profit here stays
    // accurate even if a product's current cost has since changed.
    public decimal TotalCost => Lines.Sum(l => l.UnitCostAtSale * l.Quantity);
    public decimal GrossProfit => Subtotal - TotalCost;
}

public record DailySalesRow(DateTime Date, int TransactionCount, decimal Revenue, decimal Cost, decimal Profit);

public record SalesReportSummary(
    DateTime FromDate,
    DateTime ToDate,
    int TransactionCount,
    decimal GrossRevenue,
    decimal TotalDiscount,
    decimal TotalTax,
    decimal TotalCost,
    decimal GrossProfit,
    IReadOnlyList<DailySalesRow> DailyBreakdown);

// Read-only reporting surface over completed sales - deliberately separate
// from ISaleService (which only ever writes new sales) and from
// IProductCatalogService (which is POS-facing product data), same split
// philosophy as every other service pair in this project.
public interface ISalesReportService
{
    Task<List<SaleListItem>> GetSalesHistoryAsync(DateTime fromDate, DateTime toDate);
    Task<SaleDetail?> GetSaleDetailAsync(int saleId);
    Task<SalesReportSummary> GetSalesReportAsync(DateTime fromDate, DateTime toDate);
}