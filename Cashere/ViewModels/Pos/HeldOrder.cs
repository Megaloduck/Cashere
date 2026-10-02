using System;
using System.Collections.Generic;

namespace Cashere.ViewModels.Pos;

public sealed record HeldOrderLine(
    int ProductId,
    string Name,
    decimal UnitPrice,
    decimal UnitCost,
    decimal TaxRatePercent,
    int StockAvailable,
    int Quantity,
    bool EnforceStock = true);

public sealed record HeldOrder(
    string Name,
    DateTime HeldAt,
    IReadOnlyList<HeldOrderLine> Lines,
    decimal DiscountAmount,
    string? VoucherCode,
    decimal TotalAmount,
    string OrderType = "Sale");
