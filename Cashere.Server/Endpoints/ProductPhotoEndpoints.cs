using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Data;
using Cashere.Server.Services;
using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Cashere.Server.Endpoints;

public static class ProductPhotoEndpoints
{
    // Keep this in sync with the cleanup loop below - anything not listed
    // here gets rejected before it ever touches disk.
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static void MapProductPhotoEndpoints(this WebApplication app)
    {
        // Called by the mobile labeling flow after a still capture. Expects
        // multipart/form-data with a single file field named "photo".
        app.MapPost("/api/products/{id:int}/photo", async (
            int id, HttpRequest request, CashereDbContext db, ProductPhotoStorage storage) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("Expected multipart/form-data.");
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["photo"];
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest("No photo file was provided under the 'photo' field.");
            }

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return Results.NotFound($"Product {id} was not found.");
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            ext = ext.ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
            {
                return Results.BadRequest("Unsupported image type - use JPG, PNG or WEBP.");
            }

            // One photo per product - clear out any stale file left behind
            // from a previous upload under a different extension so photos
            // don't silently orphan on disk every time a cashier retakes one.
            foreach (var oldExt in AllowedExtensions.Where(e => e != ext))
            {
                var stalePath = Path.Combine(storage.ProductsFolder, $"{id}{oldExt}");
                if (File.Exists(stalePath))
                {
                    try { File.Delete(stalePath); } catch { /* best-effort cleanup */ }
                }
            }

            var fileName = $"{id}{ext}";
            var filePath = Path.Combine(storage.ProductsFolder, fileName);

            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"products/{fileName}";
            product.PhotoPath = relativePath;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new ProductPhotoUploadResult(relativePath, $"/media/{relativePath}"));
        });
    }
}