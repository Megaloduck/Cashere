using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Cashere.Sync.Dtos;

public record CartItemDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public record CartDto(
    IReadOnlyList<CartItemDto> Items,
    decimal Subtotal,
    DateTime UpdatedAt);
