using Cashere.Data;
using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cashere.Server.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // Full catalog pull - mobile calls this once on pairing, then relies
        // on ProductCatalogChanged pushes (future work) to know when to refetch.
        // Also used by the mobile Labeling tab to pick which product to
        // photograph - HasPhoto lets that screen flag products still missing one.
        app.MapGet("/api/products", async (CashereDbContext db) =>
        {
            var products = await db.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Select(p => new ProductDto(
                    p.Id, p.Sku, p.Barcode, p.Name, p.Unit,
                    p.SellingPrice, p.StockQuantity,
                    p.Category != null ? p.Category.Name : null,
                    p.IsActive,
                    p.PhotoPath != null))
                .ToListAsync();

            return Results.Ok(products);
        });

        // Lightweight endpoint mobile hits right after scanning the pairing
        // QR code, to confirm it reached the right shop before syncing.
        app.MapGet("/api/health", async (CashereDbContext db) =>
        {
            var shop = await db.ReceiptAdmin.FirstOrDefaultAsync();
            return Results.Ok(new { shopName = shop?.ShopName ?? "Cashere", status = "ok" });
        });
    }
}