using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.SignalR;
using System.Linq;  
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Cashere.Server.Services;

// Holds the till's current in-progress cart in memory. A single active
// cart is enough for MVP (one till); shard this by till/session id if
// Cashere ever needs to run more than one register off the same server.
public class ActiveCartService
{
    private readonly object _lock = new();
    private readonly List<CartItemDto> _items = new();

    public CartDto GetCart()
    {
        lock (_lock)
        {
            return Snapshot();
        }
    }

    public CartDto AddItem(int productId, string productName, decimal unitPrice, int quantity)
    {
        lock (_lock)
        {
            var index = _items.FindIndex(i => i.ProductId == productId);
            if (index >= 0)
            {
                var existing = _items[index];
                var newQuantity = existing.Quantity + quantity;
                _items[index] = existing with { Quantity = newQuantity, Subtotal = existing.UnitPrice * newQuantity };
            }
            else
            {
                _items.Add(new CartItemDto(productId, productName, unitPrice, quantity, unitPrice * quantity));
            }

            return Snapshot();
        }
    }

    public CartDto Clear()
    {
        lock (_lock)
        {
            _items.Clear();
            return Snapshot();
        }
    }

    private CartDto Snapshot() =>
        new(_items.ToList(), _items.Sum(i => i.Subtotal), DateTime.UtcNow);
}
