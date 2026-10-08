using System.Collections.Generic;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public sealed record HistoricalSalesImportResult(int ImportedSales, int SkippedDuplicates);

public interface IHistoricalSalesImportService
{
    Task<byte[]> CreateTemplateAsync();
    Task<HistoricalSalesImportResult> ImportAsync(byte[] workbookBytes, int importingCashierId, UserRole role);
}
