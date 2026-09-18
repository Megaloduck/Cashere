using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Cashere.Services;
using ClosedXML.Excel;

namespace Cashere.Data.Services;

public class ReportExportService : IReportExportService
{
    private readonly ISalesReportService _reportService;
    private readonly string _reportsFolder;

    public ReportExportService(string dbPath, ISalesReportService reportService)
    {
        _reportService = reportService;
        _reportsFolder = Path.Combine(Path.GetDirectoryName(dbPath)!, "reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    public async Task<string> ExportSalesReportAsync(DateTime fromDate, DateTime toDate)
    {
        var summary = await _reportService.GetSalesReportAsync(fromDate, toDate);
        var sales = await _reportService.GetSalesHistoryAsync(fromDate, toDate);

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
        string[] saleHeaders = { "Date", "Sale #", "Cashier", "Customer", "Items", "Status", "Total" };
        for (var i = 0; i < saleHeaders.Length; i++) salesSheet.Cell(1, i + 1).Value = saleHeaders[i];
        row = 2;
        foreach (var sale in sales)
        {
            salesSheet.Cell(row, 1).Value = sale.SaleDate;
            salesSheet.Cell(row, 2).Value = sale.SaleNumber;
            salesSheet.Cell(row, 3).Value = sale.CashierName;
            salesSheet.Cell(row, 4).Value = sale.CustomerName ?? "";
            salesSheet.Cell(row, 5).Value = sale.ItemCount;
            salesSheet.Cell(row, 6).Value = sale.Status.ToString();
            salesSheet.Cell(row, 7).Value = sale.TotalAmount;
            row++;
        }
        salesSheet.Columns().AdjustToContents();

        var fileName = $"sales-report-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}-{DateTime.Now:HHmmss}.xlsx";
        var path = Path.Combine(_reportsFolder, fileName);
        workbook.SaveAs(path);
        return path;
    }
}