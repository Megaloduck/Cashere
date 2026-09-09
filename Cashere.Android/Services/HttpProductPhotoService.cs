using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using Cashere.Services;
using Cashere.Sync.Dtos;

namespace Cashere.Android.Services;

// HTTP-only companion to SignalRPosSyncClientService - product lookup and
// photo upload are plain REST calls against Cashere.Server (no SignalR hub
// involved), but still need to know which till IPosSyncClientService is
// currently connected to, hence the dependency on it here rather than
// tracking a separate endpoint.
public class HttpProductPhotoService : IProductPhotoService
{
    private readonly IPosSyncClientService _syncClient;
    private readonly HttpClient _httpClient = new();

    public HttpProductPhotoService(IPosSyncClientService syncClient)
    {
        _syncClient = syncClient;
    }

    public async Task<IReadOnlyList<ProductLookupItem>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var baseAddress = GetBaseAddressOrThrow();

        var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>(
            $"{baseAddress}/api/products", cancellationToken);

        return (products ?? new List<ProductDto>())
            .Select(p => new ProductLookupItem(p.Id, p.Sku, p.Barcode, p.Name, p.HasPhoto))
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<PhotoUploadOutcome> UploadPhotoAsync(
        int productId, byte[] imageBytes, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseAddress = GetBaseAddressOrThrow();

            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "photo", fileName);

            var response = await _httpClient.PostAsync(
                $"{baseAddress}/api/products/{productId}/photo", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new PhotoUploadOutcome(false, null, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }

            var result = await response.Content.ReadFromJsonAsync<ProductPhotoUploadResult>(cancellationToken: cancellationToken);
            return new PhotoUploadOutcome(true, result?.PhotoUrl, null);
        }
        catch (Exception ex)
        {
            return new PhotoUploadOutcome(false, null, ex.Message);
        }
    }

    private string GetBaseAddressOrThrow()
    {
        var endpoint = _syncClient.CurrentEndpoint
            ?? throw new InvalidOperationException("Not connected to a till.");
        return $"http://{endpoint.Host}:{endpoint.Port}";
    }
}