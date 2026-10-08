using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.SignalR;
using System.Linq;  
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Cashere.Services;

namespace Cashere.Server.Services;

// Holds the till's current in-progress cart in memory. A single active
// cart is enough for MVP (one till); shard this by till/session id if
// Cashere ever needs to run more than one register off the same server.
public class ActiveCartService : IActiveCartBridge
{
    private readonly object _lock = new();
    private readonly List<CartItemDto> _items = new();

    public event Action<MobileCartMutation>? MobileCartChanged;

    public CartDto GetCart()
    {
        lock (_lock)
        {
            return Snapshot();
        }
    }

    public CartDto AddItem(int productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) return GetCart();

        CartDto cart;
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

            cart = Snapshot();
        }

        MobileCartChanged?.Invoke(new MobileCartMutation(MobileCartMutationKind.Add, productId, quantity));
        return cart;
    }

    public CartDto DecrementItem(int productId)
    {
        CartDto cart;
        var changed = false;
        lock (_lock)
        {
            var index = _items.FindIndex(i => i.ProductId == productId);
            if (index >= 0)
            {
                changed = true;
                var existing = _items[index];
                if (existing.Quantity <= 1)
                    _items.RemoveAt(index);
                else
                    _items[index] = existing with { Quantity = existing.Quantity - 1, Subtotal = existing.UnitPrice * (existing.Quantity - 1) };
            }
            cart = Snapshot();
        }

        if (changed) MobileCartChanged?.Invoke(new MobileCartMutation(MobileCartMutationKind.Decrement, productId));
        return cart;
    }

    public CartDto RemoveItem(int productId)
    {
        CartDto cart;
        var changed = false;
        lock (_lock)
        {
            changed = _items.RemoveAll(i => i.ProductId == productId) > 0;
            cart = Snapshot();
        }

        if (changed) MobileCartChanged?.Invoke(new MobileCartMutation(MobileCartMutationKind.Remove, productId));
        return cart;
    }

    public CartDto Clear()
    {
        var changed = false;
        CartDto cart;
        lock (_lock)
        {
            changed = _items.Count > 0;
            _items.Clear();
            cart = Snapshot();
        }

        if (changed) MobileCartChanged?.Invoke(new MobileCartMutation(MobileCartMutationKind.Clear));
        return cart;
    }

    private CartDto Snapshot() =>
        new(_items.ToList(), _items.Sum(i => i.Subtotal), DateTime.UtcNow);
}
