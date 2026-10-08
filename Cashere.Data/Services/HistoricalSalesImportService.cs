using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public sealed class HistoricalSalesImportService : IHistoricalSalesImportService
{
    private readonly IDbContextFactory<CashereDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;
    public HistoricalSalesImportService(IDbContextFactory<CashereDbContext> dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public Task<byte[]> CreateTemplateAsync()
    {
        using var workbook = new XLWorkbook();
        HistoricalSalesWorkbookSchema.AddGuide(workbook);
        foreach (var (name, headers) in new[]
        {
            (HistoricalSalesWorkbookSchema.SalesSheet, HistoricalSalesWorkbookSchema.SalesHeaders),
            (HistoricalSalesWorkbookSchema.ItemsSheet, HistoricalSalesWorkbookSchema.ItemHeaders),
            (HistoricalSalesWorkbookSchema.PaymentsSheet, HistoricalSalesWorkbookSchema.PaymentHeaders),
            (HistoricalSalesWorkbookSchema.RefundsSheet, HistoricalSalesWorkbookSchema.RefundHeaders),
            (HistoricalSalesWorkbookSchema.RefundItemsSheet, HistoricalSalesWorkbookSchema.RefundItemHeaders)
        })
            HistoricalSalesWorkbookSchema.AddImportSheet(workbook, name, headers);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return Task.FromResult(output.ToArray());
    }

    public async Task<HistoricalSalesImportResult> ImportAsync(byte[] workbookBytes, int importingCashierId, UserRole role)
    {
        if (role != UserRole.Owner) throw new InvalidOperationException("Only the Owner can import historical sales.");
        if (workbookBytes.Length == 0 || workbookBytes.Length > 25 * 1024 * 1024)
            throw new InvalidOperationException("Choose a non-empty XLSX file smaller than 25 MB.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var importingCashier = await db.Cashiers.SingleOrDefaultAsync(c => c.Id == importingCashierId && c.IsActive && c.Role == UserRole.Owner);
        if (importingCashier is null) throw new InvalidOperationException("The signed-in Owner account could not be verified.");

        using var input = new MemoryStream(workbookBytes, writable: false);
        using var workbook = new XLWorkbook(input);
        var salesSheet = GetSheet(workbook, HistoricalSalesWorkbookSchema.SalesSheet, HistoricalSalesWorkbookSchema.SalesHeaders);
        var itemsSheet = GetSheet(workbook, HistoricalSalesWorkbookSchema.ItemsSheet, HistoricalSalesWorkbookSchema.ItemHeaders);
        var paymentsSheet = GetSheet(workbook, HistoricalSalesWorkbookSchema.PaymentsSheet, HistoricalSalesWorkbookSchema.PaymentHeaders);
        var refundsSheet = GetSheet(workbook, HistoricalSalesWorkbookSchema.RefundsSheet, HistoricalSalesWorkbookSchema.RefundHeaders);
        var refundItemsSheet = GetSheet(workbook, HistoricalSalesWorkbookSchema.RefundItemsSheet, HistoricalSalesWorkbookSchema.RefundItemHeaders);

        var saleRows = ReadSales(salesSheet);
        if (saleRows.Count == 0) throw new InvalidOperationException("The Import Sales sheet has no sales to import.");
        foreach (var row in saleRows)
        {
            if (row.Number.Length > 32 || string.IsNullOrWhiteSpace(row.Number)) throw RowError(salesSheet.Name, row.SourceRow, "Sale # must contain 1 to 32 characters");
            if (row.OrderType.Length > 32) throw RowError(salesSheet.Name, row.SourceRow, "Order Type must be 32 characters or fewer");
            if (row.CashierUsername.Length > 64 || row.CashierName.Length > 100) throw RowError(salesSheet.Name, row.SourceRow, "cashier username or display name is too long");
            if (row.CustomerName.Length > 100 || row.CustomerPhone.Length > 40 || row.CustomerEmail.Length > 254) throw RowError(salesSheet.Name, row.SourceRow, "customer name or contact details are too long");
            if (row.Subtotal < 0 || row.Discount < 0 || row.Tax < 0 || row.Total < 0) throw RowError(salesSheet.Name, row.SourceRow, "sale amounts cannot be negative");
        }
        var importedNumbers = saleRows.Select(s => s.Number).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (importedNumbers.Count != saleRows.Count) throw new InvalidOperationException("Sale # values must be unique in the Import Sales sheet.");

        var existingNumbers = await db.Sales.AsNoTracking().Where(s => importedNumbers.Contains(s.SaleNumber)).Select(s => s.SaleNumber).ToListAsync();
        var duplicates = existingNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newSaleRows = saleRows.Where(s => !duplicates.Contains(s.Number)).ToList();
        if (newSaleRows.Count == 0) return new HistoricalSalesImportResult(0, saleRows.Count);

        var itemRows = ReadItems(itemsSheet).Where(i => !duplicates.Contains(i.SaleNumber)).ToList();
        var paymentRows = ReadPayments(paymentsSheet).Where(p => !duplicates.Contains(p.SaleNumber)).ToList();
        var refundRows = ReadRefunds(refundsSheet).Where(r => !duplicates.Contains(r.SaleNumber)).ToList();
        var refundItemRows = ReadRefundItems(refundItemsSheet).Where(r => !duplicates.Contains(r.SaleNumber)).ToList();
        var newNumbers = newSaleRows.Select(s => s.Number).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ValidateSaleReferences(itemRows.Select(r => (r.SaleNumber, r.SourceRow)), newNumbers, HistoricalSalesWorkbookSchema.ItemsSheet);
        ValidateSaleReferences(paymentRows.Select(r => (r.SaleNumber, r.SourceRow)), newNumbers, HistoricalSalesWorkbookSchema.PaymentsSheet);
        ValidateSaleReferences(refundRows.Select(r => (r.SaleNumber, r.SourceRow)), newNumbers, HistoricalSalesWorkbookSchema.RefundsSheet);
        ValidateSaleReferences(refundItemRows.Select(r => (r.SaleNumber, r.SourceRow)), newNumbers, HistoricalSalesWorkbookSchema.RefundItemsSheet);
        var refundKeys = refundRows.Select(r => (r.SaleNumber.ToUpperInvariant(), r.RefundNumber)).ToHashSet();
        foreach (var refundRow in refundRows)
            if (refundRow.RefundNumber <= 0 || refundRows.Count(r => r.SaleNumber.Equals(refundRow.SaleNumber, StringComparison.OrdinalIgnoreCase) && r.RefundNumber == refundRow.RefundNumber) > 1)
                throw RowError(HistoricalSalesWorkbookSchema.RefundsSheet, refundRow.SourceRow, "Refund # must be a unique positive number within its sale");
        foreach (var refundItem in refundItemRows)
            if (!refundKeys.Contains((refundItem.SaleNumber.ToUpperInvariant(), refundItem.RefundNumber)))
                throw RowError(HistoricalSalesWorkbookSchema.RefundItemsSheet, refundItem.SourceRow, "the sale/refund key does not exist in Import Refunds");
        var products = await db.Products.AsNoTracking().ToListAsync();
        var cashierByUsername = (await db.Cashiers.ToListAsync()).ToDictionary(c => c.Username, StringComparer.OrdinalIgnoreCase);
        var customerByIdentity = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
        foreach (var customer in await db.Customers.ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone)) customerByIdentity.TryAdd("phone:" + customer.Phone.Trim(), customer);
            if (!string.IsNullOrWhiteSpace(customer.Email)) customerByIdentity.TryAdd("email:" + customer.Email.Trim(), customer);
        }
        var itemsBySale = itemRows.GroupBy(i => i.SaleNumber, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var paymentsBySale = paymentRows.GroupBy(p => p.SaleNumber, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var refundsBySale = refundRows.GroupBy(r => r.SaleNumber, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var refundItemsByKey = refundItemRows.GroupBy(r => (r.SaleNumber.ToUpperInvariant(), r.RefundNumber)).ToDictionary(g => g.Key, g => g.ToList());

        var salesToAdd = new List<Sale>();
        foreach (var row in newSaleRows)
        {
            if (!itemsBySale.TryGetValue(row.Number, out var sourceItems) || sourceItems.Count == 0)
                throw RowError(HistoricalSalesWorkbookSchema.SalesSheet, row.SourceRow, "each sale must have at least one item row");
            var items = sourceItems.OrderBy(i => i.ItemNumber).ToList();
            if (items.Select(i => i.ItemNumber).Distinct().Count() != items.Count)
                throw RowError(HistoricalSalesWorkbookSchema.ItemsSheet, items[0].SourceRow, "Item # must be unique within a sale");

            var saleCashier = GetCashier(row.CashierUsername, row.CashierName, cashierByUsername);
            var sale = new Sale
            {
                SaleNumber = row.Number,
                OrderType = string.IsNullOrWhiteSpace(row.OrderType) ? "Sale" : row.OrderType,
                SaleDate = row.DateUtc,
                Cashier = saleCashier,
                Subtotal = row.Subtotal,
                DiscountAmount = row.Discount,
                TaxAmount = row.Tax,
                RoundingAdjustment = row.Rounding,
                TotalAmount = row.Total,
                Status = row.Status
            };
            var customer = FindOrCreateCustomer(row, customerByIdentity);
            if (customer is not null) sale.Customer = customer;

            var sourceItemByNumber = new Dictionary<int, SaleItem>();
            foreach (var itemRow in items)
            {
                var product = ResolveProduct(itemRow, products);
                if (itemRow.Quantity <= 0 || itemRow.UnitPrice < 0 || itemRow.UnitCost < 0 || itemRow.Tax < 0 || itemRow.LineTotal < 0)
                    throw RowError(HistoricalSalesWorkbookSchema.ItemsSheet, itemRow.SourceRow, "quantity must be positive and amounts cannot be negative");
                var item = new SaleItem
                {
                    Product = product,
                    Quantity = itemRow.Quantity,
                    UnitPrice = itemRow.UnitPrice,
                    UnitCostAtSale = itemRow.UnitCost,
                    TaxAmount = itemRow.Tax,
                    Subtotal = itemRow.LineTotal
                };
                sale.Items.Add(item);
                sourceItemByNumber.Add(itemRow.ItemNumber, item);
            }
            if (Math.Abs(sale.Items.Sum(i => i.Subtotal) - sale.Subtotal) > 0.01m)
                throw RowError(HistoricalSalesWorkbookSchema.SalesSheet, row.SourceRow, "Subtotal does not match the sum of item Line Total values");

            if (paymentsBySale.TryGetValue(row.Number, out var sourcePayments))
            {
                foreach (var paymentRow in sourcePayments)
                {
                    sale.Payments.Add(new Payment
                    {
                        Method = paymentRow.Method,
                        Amount = paymentRow.Amount,
                        FeeAmount = paymentRow.Fee,
                        ReferenceNumber = paymentRow.Reference,
                        PaidAt = paymentRow.PaidAtUtc
                    });
                }
                if (Math.Abs(sale.Payments.Sum(p => p.Amount) - sale.TotalAmount) > 0.01m)
                    throw RowError(HistoricalSalesWorkbookSchema.PaymentsSheet, sourcePayments[0].SourceRow, "payment amounts must add up to the sale Total");
            }
            else if (sale.Status != SaleStatus.Voided && sale.TotalAmount != 0)
                throw RowError(HistoricalSalesWorkbookSchema.SalesSheet, row.SourceRow, "a non-voided sale needs payment rows totaling its Total");

            if (refundsBySale.TryGetValue(row.Number, out var sourceRefunds))
            {
                var refundedByItem = new Dictionary<int, int>();
                foreach (var refundRow in sourceRefunds)
                {
                    var refund = new Refund
                    {
                        RefundDate = refundRow.DateUtc,
                        ProcessedByCashier = GetCashier(refundRow.CashierUsername, refundRow.CashierName, cashierByUsername),
                        IsVoid = refundRow.IsVoid,
                        TotalAmount = refundRow.Amount,
                        Reason = refundRow.Reason
                    };
                    if (!refundItemRows.Any(ri => ri.SaleNumber.Equals(row.Number, StringComparison.OrdinalIgnoreCase) && ri.RefundNumber == refundRow.RefundNumber))
                        throw RowError(HistoricalSalesWorkbookSchema.RefundsSheet, refundRow.SourceRow, "each refund needs at least one refund item row");
                    if (refundItemsByKey.TryGetValue((row.Number.ToUpperInvariant(), refundRow.RefundNumber), out var sourceRefundItems))
                    {
                        foreach (var refundItemRow in sourceRefundItems)
                        {
                            if (!sourceItemByNumber.TryGetValue(refundItemRow.ItemNumber, out var saleItem))
                                throw RowError(HistoricalSalesWorkbookSchema.RefundItemsSheet, refundItemRow.SourceRow, "Item # does not exist for this sale");
                            if (refundItemRow.Quantity <= 0 || refundItemRow.Quantity > saleItem.Quantity || refundItemRow.UnitPrice < 0 || refundItemRow.Subtotal < 0)
                                throw RowError(HistoricalSalesWorkbookSchema.RefundItemsSheet, refundItemRow.SourceRow, "refund quantity or amount is invalid");
                            refundedByItem.TryGetValue(refundItemRow.ItemNumber, out var previousQuantity);
                            if (previousQuantity + refundItemRow.Quantity > saleItem.Quantity)
                                throw RowError(HistoricalSalesWorkbookSchema.RefundItemsSheet, refundItemRow.SourceRow, "refunded quantity exceeds the sold quantity across refunds");
                            refundedByItem[refundItemRow.ItemNumber] = previousQuantity + refundItemRow.Quantity;
                            refund.Lines.Add(new RefundLineItem
                            {
                                SaleItem = saleItem,
                                Quantity = refundItemRow.Quantity,
                                UnitPrice = refundItemRow.UnitPrice,
                                Subtotal = refundItemRow.Subtotal
                            });
                        }
                    }
                    if (Math.Abs(refund.Lines.Sum(line => line.Subtotal) - refund.TotalAmount) > 0.01m)
                        throw RowError(HistoricalSalesWorkbookSchema.RefundsSheet, refundRow.SourceRow, "refund Amount does not match the sum of refund item Subtotal values");
                    sale.Refunds.Add(refund);
                }
            }
            else if (row.Status is SaleStatus.Refunded or SaleStatus.PartiallyRefunded or SaleStatus.Voided)
                throw RowError(HistoricalSalesWorkbookSchema.SalesSheet, row.SourceRow, "refunded, partially refunded, or voided sales need refund rows and refund item rows");
            salesToAdd.Add(sale);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Sales.AddRange(salesToAdd);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new HistoricalSalesImportResult(salesToAdd.Count, duplicates.Count);
    }

    private static IXLWorksheet GetSheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The workbook is missing the '{name}' sheet. Download the Cashere import template for the required format.");
        for (var i = 0; i < headers.Count; i++)
            if (!CellText(sheet.Cell(1, i + 1)).Equals(headers[i], StringComparison.Ordinal))
                throw new InvalidOperationException($"The '{name}' sheet must use the exact Cashere column headers. Column {i + 1} should be '{headers[i]}'.");
        return sheet;
    }

    private static List<SaleRow> ReadSales(IXLWorksheet sheet)
    {
        var rows = new List<SaleRow>();
        for (var r = 2; r <= sheet.LastRowUsed()?.RowNumber(); r++)
        {
            if (Enumerable.Range(1, HistoricalSalesWorkbookSchema.SalesHeaders.Length).All(c => CellText(sheet.Cell(r, c)).Length == 0)) continue;
            var statusText = RequiredText(sheet, r, 14, "Status");
            if (!Enum.TryParse<SaleStatus>(statusText, true, out var status) || !Enum.IsDefined(status)) throw RowError(sheet.Name, r, $"unknown Sale Status '{statusText}'");
            rows.Add(new SaleRow(RequiredText(sheet, r, 1, "Sale #"), CellText(sheet.Cell(r, 2)), RequiredDate(sheet, r, 3), RequiredText(sheet, r, 4, "Cashier Username"), RequiredText(sheet, r, 5, "Cashier Name"), CellText(sheet.Cell(r, 6)), CellText(sheet.Cell(r, 7)), CellText(sheet.Cell(r, 8)), RequiredDecimal(sheet, r, 9), RequiredDecimal(sheet, r, 10), RequiredDecimal(sheet, r, 11), RequiredDecimal(sheet, r, 12), RequiredDecimal(sheet, r, 13), status, r));
        }
        return rows;
    }

    private static List<ItemRow> ReadItems(IXLWorksheet sheet) => ReadRows(sheet, HistoricalSalesWorkbookSchema.ItemHeaders.Length, r => new ItemRow(RequiredText(sheet, r, 1, "Sale #"), RequiredInt(sheet, r, 2), CellText(sheet.Cell(r, 3)), CellText(sheet.Cell(r, 4)), RequiredInt(sheet, r, 5), RequiredDecimal(sheet, r, 6), RequiredDecimal(sheet, r, 7), RequiredDecimal(sheet, r, 8), RequiredDecimal(sheet, r, 9), r));
    private static List<PaymentRow> ReadPayments(IXLWorksheet sheet) => ReadRows(sheet, HistoricalSalesWorkbookSchema.PaymentHeaders.Length, r =>
    {
        var methodText = RequiredText(sheet, r, 2, "Method");
        if (!Enum.TryParse<PaymentMethod>(methodText, true, out var method) || !Enum.IsDefined(method)) throw RowError(sheet.Name, r, $"unknown payment method '{methodText}'");
        var amount = RequiredDecimal(sheet, r, 3); var fee = RequiredDecimal(sheet, r, 4);
        if (amount < 0 || fee < 0) throw RowError(sheet.Name, r, "payment amounts cannot be negative");
        return new PaymentRow(RequiredText(sheet, r, 1, "Sale #"), method, amount, fee, CellText(sheet.Cell(r, 5)), RequiredDate(sheet, r, 6), r);
    });
    private static List<RefundRow> ReadRefunds(IXLWorksheet sheet) => ReadRows(sheet, HistoricalSalesWorkbookSchema.RefundHeaders.Length, r =>
    {
        var type = RequiredText(sheet, r, 6, "Type");
        if (!type.Equals("Refund", StringComparison.OrdinalIgnoreCase) && !type.Equals("Void", StringComparison.OrdinalIgnoreCase)) throw RowError(sheet.Name, r, "Type must be Refund or Void");
        var amount = RequiredDecimal(sheet, r, 7); if (amount < 0) throw RowError(sheet.Name, r, "refund Amount cannot be negative");
        return new RefundRow(RequiredText(sheet, r, 1, "Sale #"), RequiredInt(sheet, r, 2), RequiredDate(sheet, r, 3), RequiredText(sheet, r, 4, "Processed By Username"), RequiredText(sheet, r, 5, "Processed By Name"), type.Equals("Void", StringComparison.OrdinalIgnoreCase), amount, CellText(sheet.Cell(r, 8)), r);
    });
    private static List<RefundItemRow> ReadRefundItems(IXLWorksheet sheet) => ReadRows(sheet, HistoricalSalesWorkbookSchema.RefundItemHeaders.Length, r => new RefundItemRow(RequiredText(sheet, r, 1, "Sale #"), RequiredInt(sheet, r, 2), RequiredInt(sheet, r, 3), RequiredInt(sheet, r, 4), RequiredDecimal(sheet, r, 5), RequiredDecimal(sheet, r, 6), r));

    private static List<T> ReadRows<T>(IXLWorksheet sheet, int columns, Func<int, T> parse)
    {
        var result = new List<T>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            if (Enumerable.Range(1, columns).All(c => CellText(sheet.Cell(r, c)).Length == 0)) continue;
            result.Add(parse(r));
        }
        return result;
    }

    private static string CellText(IXLCell cell) => cell.IsEmpty() ? string.Empty : cell.GetFormattedString().Trim();
    private static string RequiredText(IXLWorksheet sheet, int row, int column, string label) => CellText(sheet.Cell(row, column)) is { Length: > 0 } value ? value : throw RowError(sheet.Name, row, $"{label} is required");
    private static decimal RequiredDecimal(IXLWorksheet sheet, int row, int column, string label = "amount")
    {
        var cell = sheet.Cell(row, column);
        if (cell.TryGetValue<decimal>(out var value)) return value;
        var text = CellText(cell);
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value) || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return value;
        throw RowError(sheet.Name, row, $"{label} must be a number");
    }
    private static int RequiredInt(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.TryGetValue<int>(out var value)) return value;
        if (int.TryParse(CellText(cell), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) return value;
        throw RowError(sheet.Name, row, "expected a whole number");
    }
    private static DateTime RequiredDate(IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
        if (cell.TryGetValue<DateTime>(out var value)) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        if (DateTime.TryParse(CellText(cell), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value)) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        throw RowError(sheet.Name, row, "timestamp must be a valid UTC date and time");
    }
    private static InvalidOperationException RowError(string sheet, int row, string message) => new($"{sheet}, row {row}: {message}.");

    private static void ValidateSaleReferences(IEnumerable<(string SaleNumber, int SourceRow)> rows, HashSet<string> saleNumbers, string sheetName)
    {
        foreach (var row in rows)
            if (!saleNumbers.Contains(row.SaleNumber)) throw RowError(sheetName, row.SourceRow, $"Sale # '{row.SaleNumber}' does not exist in Import Sales");
    }

    private static Product ResolveProduct(ItemRow row, List<Product> products)
    {
        var matches = !string.IsNullOrWhiteSpace(row.Sku)
            ? products.Where(p => p.Sku.Equals(row.Sku, StringComparison.OrdinalIgnoreCase)).ToList()
            : products.Where(p => p.Name.Equals(row.ProductName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count != 1) throw RowError(HistoricalSalesWorkbookSchema.ItemsSheet, row.SourceRow, matches.Count == 0 ? $"Cashere product '{row.Sku}' / '{row.ProductName}' was not found" : $"product '{row.Sku}' matches more than one catalog record");
        return matches[0];
    }

    private Cashier GetCashier(string username, string displayName, Dictionary<string, Cashier> cashiers)
    {
        if (cashiers.TryGetValue(username, out var existing)) return existing;
        if (string.IsNullOrWhiteSpace(displayName)) throw new InvalidOperationException($"Cashier '{username}' is not in Cashere and has no historical display name.");
        var inactive = new Cashier
        {
            Username = username,
            DisplayName = displayName,
            Role = UserRole.Cashier,
            IsActive = false,
            PasswordHash = _passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
        };
        cashiers.Add(username, inactive);
        return inactive;
    }

    private static Customer? FindOrCreateCustomer(SaleRow row, Dictionary<string, Customer> customers)
    {
        var keys = new[] { string.IsNullOrWhiteSpace(row.CustomerPhone) ? null : "phone:" + row.CustomerPhone.Trim(), string.IsNullOrWhiteSpace(row.CustomerEmail) ? null : "email:" + row.CustomerEmail.Trim() }.Where(k => k is not null).Cast<string>().ToList();
        foreach (var key in keys) if (customers.TryGetValue(key, out var found)) return found;
        if (string.IsNullOrWhiteSpace(row.CustomerName)) return null;
        var customer = new Customer { Name = row.CustomerName, Phone = NullIfBlank(row.CustomerPhone), Email = NullIfBlank(row.CustomerEmail) };
        foreach (var key in keys) customers.TryAdd(key, customer);
        return customer;
    }
    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record SaleRow(string Number, string OrderType, DateTime DateUtc, string CashierUsername, string CashierName, string CustomerName, string CustomerPhone, string CustomerEmail, decimal Subtotal, decimal Discount, decimal Tax, decimal Rounding, decimal Total, SaleStatus Status, int SourceRow);
    private sealed record ItemRow(string SaleNumber, int ItemNumber, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal UnitCost, decimal Tax, decimal LineTotal, int SourceRow);
    private sealed record PaymentRow(string SaleNumber, PaymentMethod Method, decimal Amount, decimal Fee, string Reference, DateTime PaidAtUtc, int SourceRow);
    private sealed record RefundRow(string SaleNumber, int RefundNumber, DateTime DateUtc, string CashierUsername, string CashierName, bool IsVoid, decimal Amount, string Reason, int SourceRow);
    private sealed record RefundItemRow(string SaleNumber, int RefundNumber, int ItemNumber, int Quantity, decimal UnitPrice, decimal Subtotal, int SourceRow);
}
