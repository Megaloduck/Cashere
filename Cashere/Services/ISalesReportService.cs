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

public record RefundHistoryEntry(DateTime RefundDate, string ProcessedByName, decimal TotalAmount, bool IsVoid, string? Reason);

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
    IReadOnlyList<SalePaymentDetail> Payments,
    IReadOnlyList<RefundHistoryEntry> Refunds)
{
    public decimal TotalCost => Lines.Sum(l => l.UnitCostAtSale * l.Quantity);
    public decimal GrossProfit => Subtotal - TotalCost;
    public decimal TotalRefunded => Refunds.Sum(r => r.TotalAmount);
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

public interface ISalesReportService
{
    Task<List<SaleListItem>> GetSalesHistoryAsync(DateTime fromDate, DateTime toDate);
    Task<SaleDetail?> GetSaleDetailAsync(int saleId);
    Task<SalesReportSummary> GetSalesReportAsync(DateTime fromDate, DateTime toDate);
}