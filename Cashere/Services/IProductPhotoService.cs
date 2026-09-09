using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Cashere.Services;

public record ProductLookupItem(int Id, string Sku, string? Barcode, string Name, bool HasPhoto);

public record PhotoUploadOutcome(bool Success, string? PhotoUrl, string? ErrorMessage);

// Implemented in Cashere.Android using HttpClient - talks to the same till
// IPosSyncClientService is connected to (Cashere.Server's /api/products and
// /api/products/{id}/photo endpoints), so it depends on IPosSyncClientService
// for the current host/port rather than tracking its own connection state.
public interface IProductPhotoService
{
    Task<IReadOnlyList<ProductLookupItem>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<PhotoUploadOutcome> UploadPhotoAsync(
        int productId, byte[] imageBytes, string fileName, CancellationToken cancellationToken = default);
}