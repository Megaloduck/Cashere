using Cashere.Data;
using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Server.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // Full catalog pull - mobile calls this once on pairing, then relies
        // on ProductCatalogChanged pushes (future work) to know when to refetch.
        app.MapGet("/api/products", async (CashereDbContext db) =>
        {
            var products = await db.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Select(p => new ProductDto(
                    p.Id, p.Sku, p.Barcode, p.Name, p.Unit,
                    p.SellingPrice, p.StockQuantity,
                    p.Category != null ? p.Category.Name : null,
                    p.IsActive))
                .ToListAsync();

            return Results.Ok(products);
        });

        // Lightweight endpoint mobile hits right after scanning the pairing
        // QR code, to confirm it reached the right shop before syncing.
        app.MapGet("/api/health", async (CashereDbContext db) =>
        {
            var shop = await db.ShopSettings.FirstOrDefaultAsync();
            return Results.Ok(new { shopName = shop?.ShopName ?? "Cashere", status = "ok" });
        });
    }
}
