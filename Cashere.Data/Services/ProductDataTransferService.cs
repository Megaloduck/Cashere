using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;

namespace Cashere.Data.Services;

/// <summary>CSV catalog transfer. Matching SKU rows update catalog fields but retain current on-hand stock.</summary>
public sealed class ProductDataTransferService : IProductDataTransferService
{
    private static readonly string[] Headers =
    [
        "SKU", "Barcode", "Name", "Category", "Unit", "CostPrice", "SellingPrice",
        "StockQuantity", "LowStockThreshold", "Active", "OutOfStockBehavior", "TaxRateOverridePercent"
    ];

    private readonly IProductAdminService _products;
    private readonly ICategoryAdminService _categories;

    public ProductDataTransferService(IProductAdminService products, ICategoryAdminService categories)
    {
        _products = products;
        _categories = categories;
    }

    public async Task<string> ExportCsvAsync()
    {
        var products = await _products.GetAllProductsAsync();
        var output = new StringBuilder();
        output.AppendLine(string.Join(',', Headers));
        foreach (var product in products)
        {
            var values = new string?[]
            {
                product.Sku, product.Barcode, product.Name, product.Category?.Name, product.Unit,
                product.CostPrice.ToString(CultureInfo.InvariantCulture),
                product.SellingPrice.ToString(CultureInfo.InvariantCulture),
                product.StockQuantity.ToString(CultureInfo.InvariantCulture),
                product.LowStockThreshold.ToString(CultureInfo.InvariantCulture),
                product.IsActive.ToString(), product.OutOfStockBehaviorOverride?.ToString(),
                product.TaxRateOverridePercent?.ToString(CultureInfo.InvariantCulture)
            };
            output.AppendLine(string.Join(',', values.Select(Escape)));
        }
        return output.ToString();
    }

    public async Task<ProductCsvImportResult> ImportCsvAsync(string csvContents, bool updateExistingProducts)
    {
        if (string.IsNullOrWhiteSpace(csvContents)) throw new AdminValidationException("The CSV file is empty.");
        var rows = ParseCsv(csvContents.TrimStart('\uFEFF'));
        if (rows.Count < 2) throw new AdminValidationException("The CSV must contain a header and at least one product row.");

        var header = rows[0].Select(x => x.Trim()).ToList();
        var columns = Headers.ToDictionary(name => name, name => header.FindIndex(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)), StringComparer.Ordinal);
        var missing = Headers.Where(name => columns[name] < 0).ToArray();
        if (missing.Length > 0) throw new AdminValidationException($"Missing required CSV columns: {string.Join(", ", missing)}.");

        var existing = await _products.GetAllProductsAsync();
        var bySku = existing.GroupBy(product => product.Sku, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var categories = await _categories.GetAllCategoriesAsync();
        var categoryByName = categories.GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var added = 0;
        var updated = 0;
        var skipped = 0;
        var issues = new List<string>();

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            var line = rowIndex + 1;
            try
            {
                string Get(string name) => columns[name] < row.Count ? row[columns[name]].Trim() : string.Empty;
                var sku = Required(Get("SKU"), "SKU");
                if (!seenSkus.Add(sku))
                {
                    skipped++;
                    if (issues.Count < 20) issues.Add($"Row {line}: duplicate SKU '{sku}' in this file was skipped.");
                    continue;
                }
                var name = Required(Get("Name"), "Name");
                var unit = Required(Get("Unit"), "Unit");
                var barcode = NullIfBlank(Get("Barcode"));
                var cost = Decimal(Get("CostPrice"), "CostPrice");
                var price = Decimal(Get("SellingPrice"), "SellingPrice");
                if (cost < 0 || price < 0) throw new AdminValidationException("CostPrice and SellingPrice cannot be negative.");
                var stock = Integer(Get("StockQuantity"), "StockQuantity");
                var lowStock = Integer(Get("LowStockThreshold"), "LowStockThreshold");
                if (lowStock < 0) throw new AdminValidationException("LowStockThreshold cannot be negative.");
                var activeText = Get("Active");
                if (!bool.TryParse(activeText, out var active)) throw new AdminValidationException("Active must be true or false.");
                var behaviorText = Get("OutOfStockBehavior");
                OutOfStockBehavior? behavior = null;
                if (!string.IsNullOrWhiteSpace(behaviorText))
                {
                    if (!Enum.TryParse<OutOfStockBehavior>(behaviorText, true, out var parsedBehavior))
                        throw new AdminValidationException("OutOfStockBehavior must be Block, AllowNegativeStock, or blank.");
                    behavior = parsedBehavior;
                }
                var taxText = Get("TaxRateOverridePercent");
                decimal? tax = string.IsNullOrWhiteSpace(taxText) ? null : Decimal(taxText, "TaxRateOverridePercent");
                if (tax is < 0 or > 100) throw new AdminValidationException("TaxRateOverridePercent must be from 0 to 100.");

                int? categoryId = null;
                var categoryName = Get("Category");
                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    if (!categoryByName.TryGetValue(categoryName, out var category))
                    {
                        category = await _categories.CreateCategoryAsync(categoryName);
                        categoryByName[category.Name] = category;
                    }
                    categoryId = category.Id;
                }

                var input = new ProductInput(sku, barcode, name, categoryId, unit, cost, price,
                    stock, lowStock, behavior, tax);
                if (bySku.TryGetValue(sku, out var current))
                {
                    if (!updateExistingProducts)
                    {
                        skipped++;
                        continue;
                    }
                    // Importing a spreadsheet must never silently overwrite live stock counts.
                    input = input with { StockQuantity = current.StockQuantity };
                    await _products.UpdateProductAsync(current.Id, input);
                    await _products.SetActiveAsync(current.Id, active);
                    updated++;
                }
                else
                {
                    var created = await _products.CreateProductAsync(input);
                    if (!active) await _products.SetActiveAsync(created.Id, false);
                    bySku[created.Sku] = created;
                    added++;
                }
            }
            catch (Exception ex) when (ex is AdminValidationException or FormatException or OverflowException)
            {
                skipped++;
                if (issues.Count < 20) issues.Add($"Row {line}: {ex.Message}");
            }
        }

        var details = issues.Count == 0 ? null : string.Join(Environment.NewLine, issues);
        return new ProductCsvImportResult(added, updated, skipped, details);
    }

    private static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new AdminValidationException($"{field} is required.") : value;

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static decimal Decimal(string value, string field) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new AdminValidationException($"{field} must be a valid number using '.' as the decimal separator.");

    private static int Integer(string value, string field) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new AdminValidationException($"{field} must be a whole number.");

    private static string Escape(string? value) =>
        value is null ? string.Empty : value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static List<List<string>> ParseCsv(string csv)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"' && field.Length == 0) quoted = true;
            else if (c == ',') { record.Add(field.ToString()); field.Clear(); }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                record.Add(field.ToString()); field.Clear(); records.Add(record); record = new List<string>();
            }
            else field.Append(c);
        }
        if (quoted) throw new AdminValidationException("The CSV contains an unterminated quoted field.");
        if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
        return records;
    }
}
