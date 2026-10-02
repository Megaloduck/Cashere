using System.Threading.Tasks;

namespace Cashere.Services;

public record ProductCsvImportResult(int Added, int Updated, int Skipped, string? Details);

public interface IProductDataTransferService
{
    Task<string> ExportCsvAsync();
    Task<ProductCsvImportResult> ImportCsvAsync(string csvContents, bool updateExistingProducts);
}
