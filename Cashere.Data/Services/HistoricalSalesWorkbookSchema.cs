using System.Collections.Generic;
using ClosedXML.Excel;

namespace Cashere.Data.Services;

internal static class HistoricalSalesWorkbookSchema
{
    public const string SalesSheet = "Import Sales";
    public const string ItemsSheet = "Import Items";
    public const string PaymentsSheet = "Import Payments";
    public const string RefundsSheet = "Import Refunds";
    public const string RefundItemsSheet = "Import Refund Items";

    public static readonly string[] SalesHeaders =
    {
        "Sale #", "Order Type", "Date UTC", "Cashier Username", "Cashier Name",
        "Customer Name", "Customer Phone", "Customer Email", "Subtotal", "Discount",
        "Tax", "Rounding", "Total", "Status"
    };

    public static readonly string[] ItemHeaders =
    {
        "Sale #", "Item #", "Product SKU", "Product", "Quantity", "Unit Price",
        "Unit Cost", "Tax", "Line Total"
    };

    public static readonly string[] PaymentHeaders =
    {
        "Sale #", "Method", "Amount", "Fee", "Reference", "Paid At UTC"
    };

    public static readonly string[] RefundHeaders =
    {
        "Sale #", "Refund #", "Refund Date UTC", "Processed By Username",
        "Processed By Name", "Type", "Amount", "Reason"
    };

    public static readonly string[] RefundItemHeaders =
    {
        "Sale #", "Refund #", "Item #", "Quantity", "Unit Price", "Subtotal"
    };

    public static IXLWorksheet AddImportSheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers)
    {
        var sheet = workbook.Worksheets.Add(name);
        for (var column = 0; column < headers.Count; column++)
            sheet.Cell(1, column + 1).Value = headers[column];
        sheet.SheetView.FreezeRows(1);
        sheet.Row(1).Style.Font.Bold = true;
        return sheet;
    }

    public static void AddGuide(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Import Guide");
        sheet.Cell(1, 1).Value = "Cashere Historical Sales Import";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(3, 1).Value = "Fill the Import Sales, Import Items and Import Payments sheets. Refund sheets are required only when importing refunded/voided sales.";
        sheet.Cell(4, 1).Value = "Use one row per sale, sale item, payment, refund and refunded item. Keep the exact column headers and unique sale numbers.";
        sheet.Cell(5, 1).Value = "Use UTC timestamps (yyyy-MM-dd HH:mm:ss). Payment Method must be Cash, Qris, Edc, or another Cashere PaymentMethod value.";
        sheet.Cell(6, 1).Value = "Map products by Cashere SKU. If SKU is blank, product name must match exactly one product. Cashier usernames may match existing accounts; unknown historical cashiers are added as inactive history-only accounts.";
        sheet.Cell(7, 1).Value = "Customer phone/email are used to match an existing customer; otherwise a historical customer record is created when a name is supplied.";
        sheet.Cell(8, 1).Value = "Historical import preserves the values entered here and does not change current product stock. Duplicate sale numbers already in Cashere are skipped.";
        sheet.Cell(9, 1).Value = "For foreign POS systems, map the source export into these sheets first. Review tax totals and payment/refund amounts before importing.";
        sheet.Column(1).Width = 115;
        sheet.Column(1).Style.Alignment.WrapText = true;
        sheet.Rows(3, 9).Height = 34;
    }
}
