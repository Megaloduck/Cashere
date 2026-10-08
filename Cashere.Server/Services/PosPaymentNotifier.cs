using Cashere.Services;
using Cashere.Server.Hubs;
using Cashere.Sync.Dtos;
using Cashere.Sync.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Cashere.Server.Services;

public sealed class PosPaymentNotifier : IPosPaymentNotifier
{
    private readonly IHubContext<PosSyncHub, IPosSyncClient> _hubContext;
    private readonly ActiveCartService _cart;

    public PosPaymentNotifier(IHubContext<PosSyncHub, IPosSyncClient> hubContext, ActiveCartService cart)
    {
        _hubContext = hubContext;
        _cart = cart;
    }

    public async Task NotifySaleCompletedAsync(string saleNumber, decimal amount)
    {
        var cart = _cart.Clear();
        await _hubContext.Clients.All.CartUpdated(cart);
        await _hubContext.Clients.All.PaymentNotification(new PaymentNotificationDto(saleNumber, amount, DateTime.UtcNow));
    }
}
