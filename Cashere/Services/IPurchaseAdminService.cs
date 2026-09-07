using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;


namespace Cashere.Services;

public record PurchaseLineInput(int ProductId, int Quantity, decimal UnitCost);

public record PurchaseInput(
    int SupplierId,
    int CreatedByCashierId,
    string? ReferenceNumber,
    string? Notes,
    IReadOnlyList<PurchaseLineInput> Lines);

// Unlike Product/Supplier admin, purchases are treated as an immutable ledger
// entry once saved - no update/delete here, mirroring how ISaleService only
// ever completes new sales. Creating a purchase increases each line's
// Product.StockQuantity and writes a matching InventoryMovement
// (PurchaseReceived) - the mirror image of what ISaleService does on the way out.
public interface IPurchaseAdminService
{
    Task<List<Purchase>> GetAllPurchasesAsync();
    Task<Purchase> CreatePurchaseAsync(PurchaseInput input);
}