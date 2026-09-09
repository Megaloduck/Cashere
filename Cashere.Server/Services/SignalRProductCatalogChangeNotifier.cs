using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Server.Hubs;
using Cashere.Services;
using Cashere.Sync.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cashere.Server.Services;

public class SignalRProductCatalogChangeNotifier : IProductCatalogChangeNotifier
{
    private readonly IHubContext<PosSyncHub, IPosSyncClient> _hubContext;

    public SignalRProductCatalogChangeNotifier(IHubContext<PosSyncHub, IPosSyncClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyChangedAsync() => _hubContext.Clients.All.ProductCatalogChanged();
}