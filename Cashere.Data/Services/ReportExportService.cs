using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Cashere.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class ReportExportService : IReportExportService
{
    private readonly ISalesReportService _reportService;
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;
    private readonly string _reportsFolder;

    public ReportExportService(string dbPath, ISalesReportService reportService, IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _reportService = reportService;
        _dbContextFactory = dbContextFactory;
        _reportsFolder = Path.Combine(Path.GetDirectoryName(dbPath)!, "reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    public async Task<string> ExportSalesReportAsync(DateTime fromDate, DateTime toDate)
    {
        var summary = await _reportService.GetSalesReportAsync(fromDate, toDate);
        var sales = await _reportService.GetSalesHistoryAsync(fromDate, toDate);
        var (from, toExclusive) = (fromDate.Date, toDate.Date.AddDays(1));
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var detailedSales = await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate < toExclusive)
            .Include(s => s.Cashier)
            .Include(s => s.Customer)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Items).ThenInclude(i => i.RefundLineItems)
            .Include(s => s.Payments)
            .Include(s => s.Refunds).ThenInclude(r => r.ProcessedByCashier)
            .Include(s => s.Refunds).ThenInclude(r => r.Lines)
            .OrderBy(s => s.SaleDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = "Shop Sales Report";
        summarySheet.Cell(2, 1).Value = "From";
        summarySheet.Cell(2, 2).Value = summary.FromDate;
        summarySheet.Cell(3, 1).Value = "To";
        summarySheet.Cell(3, 2).Value = summary.ToDate;
        summarySheet.Cell(5, 1).Value = "Transactions";
        summarySheet.Cell(5, 2).Value = summary.TransactionCount;
        summarySheet.Cell(6, 1).Value = "Gross Revenue";
        summarySheet.Cell(6, 2).Value = summary.GrossRevenue;
        summarySheet.Cell(7, 1).Value = "Total Discount";
        summarySheet.Cell(7, 2).Value = summary.TotalDiscount;
        summarySheet.Cell(8, 1).Value = "Total Tax";
        summarySheet.Cell(8, 2).Value = summary.TotalTax;
        summarySheet.Cell(9, 1).Value = "Total Cost";
        summarySheet.Cell(9, 2).Value = summary.TotalCost;
        summarySheet.Cell(10, 1).Value = "Gross Profit";
        summarySheet.Cell(10, 2).Value = summary.GrossProfit;
        summarySheet.Columns().AdjustToContents();

        var dailySheet = workbook.Worksheets.Add("Daily Breakdown");
        string[] dailyHeaders = { "Date", "Transactions", "Revenue", "Cost", "Profit" };
        for (var i = 0; i < dailyHeaders.Length; i++) dailySheet.Cell(1, i + 1).Value = dailyHeaders[i];
        var row = 2;
        foreach (var day in summary.DailyBreakdown)
        {
            dailySheet.Cell(row, 1).Value = day.Date;
            dailySheet.Cell(row, 2).Value = day.TransactionCount;
            dailySheet.Cell(row, 3).Value = day.Revenue;
            dailySheet.Cell(row, 4).Value = day.Cost;
            dailySheet.Cell(row, 5).Value = day.Profit;
            row++;
        }
        dailySheet.Columns().AdjustToContents();

        var salesSheet = workbook.Worksheets.Add("Sales");
        string[] saleHeaders = { "Date", "Sale #", "Order Type", "Cashier", "Customer", "Items", "Status", "Total" };
        for (var i = 0; i < saleHeaders.Length; i++) salesSheet.Cell(1, i + 1).Value = saleHeaders[i];
        row = 2;
        foreach (var sale in sales)
        {
            salesSheet.Cell(row, 1).Value = sale.SaleDate;
            salesSheet.Cell(row, 2).Value = sale.SaleNumber;
            salesSheet.Cell(row, 3).Value = sale.OrderType;
            salesSheet.Cell(row, 4).Value = sale.CashierName;
            salesSheet.Cell(row, 5).Value = sale.CustomerName ?? "";
            salesSheet.Cell(row, 6).Value = sale.ItemCount;
            salesSheet.Cell(row, 7).Value = sale.Status.ToString();
            salesSheet.Cell(row, 8).Value = sale.TotalAmount;
            row++;
        }
        salesSheet.Columns().AdjustToContents();

        var itemsSheet = workbook.Worksheets.Add("Sale Items");
        string[] itemHeaders = { "Sale #", "Date", "Cashier", "SKU", "Product", "Quantity", "Refunded Quantity", "Unit Price", "Unit Cost", "Tax", "Line Total" };
        for (var i = 0; i < itemHeaders.Length; i++) itemsSheet.Cell(1, i + 1).Value = itemHeaders[i];
        row = 2;
        foreach (var sale in detailedSales)
        foreach (var item in sale.Items)
        {
            itemsSheet.Cell(row, 1).Value = sale.SaleNumber;
            itemsSheet.Cell(row, 2).Value = sale.SaleDate;
            itemsSheet.Cell(row, 3).Value = sale.Cashier.DisplayName;
            itemsSheet.Cell(row, 4).Value = item.Product.Sku;
            itemsSheet.Cell(row, 5).Value = item.Product.Name;
            itemsSheet.Cell(row, 6).Value = item.Quantity;
            itemsSheet.Cell(row, 7).Value = item.RefundLineItems.Sum(refund => refund.Quantity);
            itemsSheet.Cell(row, 8).Value = item.UnitPrice;
            itemsSheet.Cell(row, 9).Value = item.UnitCostAtSale;
            itemsSheet.Cell(row, 10).Value = item.TaxAmount;
            itemsSheet.Cell(row, 11).Value = item.Subtotal;
            row++;
        }
        itemsSheet.Columns().AdjustToContents();

        var paymentsSheet = workbook.Worksheets.Add("Payments");
        string[] paymentHeaders = { "Sale #", "Date", "Cashier", "Method", "Amount", "Fee" };
        for (var i = 0; i < paymentHeaders.Length; i++) paymentsSheet.Cell(1, i + 1).Value = paymentHeaders[i];
        row = 2;
        foreach (var sale in detailedSales)
        foreach (var payment in sale.Payments)
        {
            paymentsSheet.Cell(row, 1).Value = sale.SaleNumber;
            paymentsSheet.Cell(row, 2).Value = sale.SaleDate;
            paymentsSheet.Cell(row, 3).Value = sale.Cashier.DisplayName;
            paymentsSheet.Cell(row, 4).Value = payment.Method.ToString();
            paymentsSheet.Cell(row, 5).Value = payment.Amount;
            paymentsSheet.Cell(row, 6).Value = payment.FeeAmount;
            row++;
        }
        paymentsSheet.Columns().AdjustToContents();

        var refundsSheet = workbook.Worksheets.Add("Refunds");
        string[] refundHeaders = { "Sale #", "Refund Date", "Processed By", "Type", "Amount", "Reason" };
        for (var i = 0; i < refundHeaders.Length; i++) refundsSheet.Cell(1, i + 1).Value = refundHeaders[i];
        row = 2;
        foreach (var sale in detailedSales)
        foreach (var refund in sale.Refunds)
        {
            refundsSheet.Cell(row, 1).Value = sale.SaleNumber;
            refundsSheet.Cell(row, 2).Value = refund.RefundDate;
            refundsSheet.Cell(row, 3).Value = refund.ProcessedByCashier.DisplayName;
            refundsSheet.Cell(row, 4).Value = refund.IsVoid ? "Void" : "Refund";
            refundsSheet.Cell(row, 5).Value = refund.TotalAmount;
            refundsSheet.Cell(row, 6).Value = refund.Reason ?? string.Empty;
            row++;
        }
        refundsSheet.Columns().AdjustToContents();

        AddHistoricalImportSheets(workbook, detailedSales);

        var fileName = $"sales-report-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}-{DateTime.Now:HHmmss}.xlsx";
        var path = Path.Combine(_reportsFolder, fileName);
        workbook.SaveAs(path);
        return path;
    }

    private static void AddHistoricalImportSheets(XLWorkbook workbook, IReadOnlyList<Cashere.Models.Sale> sales)
    {
        HistoricalSalesWorkbookSchema.AddGuide(workbook);
        var salesSheet = HistoricalSalesWorkbookSchema.AddImportSheet(workbook, HistoricalSalesWorkbookSchema.SalesSheet, HistoricalSalesWorkbookSchema.SalesHeaders);
        var itemsSheet = HistoricalSalesWorkbookSchema.AddImportSheet(workbook, HistoricalSalesWorkbookSchema.ItemsSheet, HistoricalSalesWorkbookSchema.ItemHeaders);
        var paymentsSheet = HistoricalSalesWorkbookSchema.AddImportSheet(workbook, HistoricalSalesWorkbookSchema.PaymentsSheet, HistoricalSalesWorkbookSchema.PaymentHeaders);
        var refundsSheet = HistoricalSalesWorkbookSchema.AddImportSheet(workbook, HistoricalSalesWorkbookSchema.RefundsSheet, HistoricalSalesWorkbookSchema.RefundHeaders);
        var refundItemsSheet = HistoricalSalesWorkbookSchema.AddImportSheet(workbook, HistoricalSalesWorkbookSchema.RefundItemsSheet, HistoricalSalesWorkbookSchema.RefundItemHeaders);
        var salesRow = 2; var itemsRow = 2; var paymentsRow = 2; var refundsRow = 2; var refundItemsRow = 2;
        foreach (var sale in sales)
        {
            salesSheet.Cell(salesRow, 1).Value = sale.SaleNumber;
            salesSheet.Cell(salesRow, 2).Value = sale.OrderType;
            salesSheet.Cell(salesRow, 3).Value = DateTime.SpecifyKind(sale.SaleDate, DateTimeKind.Utc);
            salesSheet.Cell(salesRow, 4).Value = sale.Cashier.Username;
            salesSheet.Cell(salesRow, 5).Value = sale.Cashier.DisplayName;
            salesSheet.Cell(salesRow, 6).Value = sale.Customer?.Name ?? "";
            salesSheet.Cell(salesRow, 7).Value = sale.Customer?.Phone ?? "";
            salesSheet.Cell(salesRow, 8).Value = sale.Customer?.Email ?? "";
            salesSheet.Cell(salesRow, 9).Value = sale.Subtotal;
            salesSheet.Cell(salesRow, 10).Value = sale.DiscountAmount;
            salesSheet.Cell(salesRow, 11).Value = sale.TaxAmount;
            salesSheet.Cell(salesRow, 12).Value = sale.RoundingAdjustment;
            salesSheet.Cell(salesRow, 13).Value = sale.TotalAmount;
            salesSheet.Cell(salesRow, 14).Value = sale.Status.ToString();
            salesRow++;

            var orderedItems = sale.Items.OrderBy(item => item.Id).ToList();
            var itemOrdinals = new Dictionary<int, int>();
            for (var index = 0; index < orderedItems.Count; index++)
            {
                var item = orderedItems[index];
                itemOrdinals[item.Id] = index + 1;
                itemsSheet.Cell(itemsRow, 1).Value = sale.SaleNumber;
                itemsSheet.Cell(itemsRow, 2).Value = index + 1;
                itemsSheet.Cell(itemsRow, 3).Value = item.Product.Sku;
                itemsSheet.Cell(itemsRow, 4).Value = item.Product.Name;
                itemsSheet.Cell(itemsRow, 5).Value = item.Quantity;
                itemsSheet.Cell(itemsRow, 6).Value = item.UnitPrice;
                itemsSheet.Cell(itemsRow, 7).Value = item.UnitCostAtSale;
                itemsSheet.Cell(itemsRow, 8).Value = item.TaxAmount;
                itemsSheet.Cell(itemsRow, 9).Value = item.Subtotal;
                itemsRow++;
            }
            foreach (var payment in sale.Payments.OrderBy(payment => payment.Id))
            {
                paymentsSheet.Cell(paymentsRow, 1).Value = sale.SaleNumber;
                paymentsSheet.Cell(paymentsRow, 2).Value = payment.Method.ToString();
                paymentsSheet.Cell(paymentsRow, 3).Value = payment.Amount;
                paymentsSheet.Cell(paymentsRow, 4).Value = payment.FeeAmount;
                paymentsSheet.Cell(paymentsRow, 5).Value = payment.ReferenceNumber ?? "";
                paymentsSheet.Cell(paymentsRow, 6).Value = DateTime.SpecifyKind(payment.PaidAt, DateTimeKind.Utc);
                paymentsRow++;
            }
            var refundOrdinals = new Dictionary<int, int>();
            var orderedRefunds = sale.Refunds.OrderBy(refund => refund.Id).ToList();
            for (var index = 0; index < orderedRefunds.Count; index++)
            {
                var refund = orderedRefunds[index];
                refundOrdinals[refund.Id] = index + 1;
                refundsSheet.Cell(refundsRow, 1).Value = sale.SaleNumber;
                refundsSheet.Cell(refundsRow, 2).Value = index + 1;
                refundsSheet.Cell(refundsRow, 3).Value = DateTime.SpecifyKind(refund.RefundDate, DateTimeKind.Utc);
                refundsSheet.Cell(refundsRow, 4).Value = refund.ProcessedByCashier.Username;
                refundsSheet.Cell(refundsRow, 5).Value = refund.ProcessedByCashier.DisplayName;
                refundsSheet.Cell(refundsRow, 6).Value = refund.IsVoid ? "Void" : "Refund";
                refundsSheet.Cell(refundsRow, 7).Value = refund.TotalAmount;
                refundsSheet.Cell(refundsRow, 8).Value = refund.Reason ?? "";
                refundsRow++;
                foreach (var line in refund.Lines.OrderBy(line => line.Id))
                {
                    if (!itemOrdinals.TryGetValue(line.SaleItemId, out var itemOrdinal)) continue;
                    refundItemsSheet.Cell(refundItemsRow, 1).Value = sale.SaleNumber;
                    refundItemsSheet.Cell(refundItemsRow, 2).Value = index + 1;
                    refundItemsSheet.Cell(refundItemsRow, 3).Value = itemOrdinal;
                    refundItemsSheet.Cell(refundItemsRow, 4).Value = line.Quantity;
                    refundItemsSheet.Cell(refundItemsRow, 5).Value = line.UnitPrice;
                    refundItemsSheet.Cell(refundItemsRow, 6).Value = line.Subtotal;
                    refundItemsRow++;
                }
            }
        }
        foreach (var sheet in new[] { salesSheet, itemsSheet, paymentsSheet, refundsSheet, refundItemsSheet }) sheet.Columns().AdjustToContents();
    }
}
