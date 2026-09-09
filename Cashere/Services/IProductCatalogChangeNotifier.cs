using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// Fire-and-forget notification broadcast to every connected mobile client
// whenever an admin-driven write makes the mobile catalog projection
// (Cashere.Sync.Dtos.ProductDto) stale - a new/edited product, an
// activate/deactivate toggle, a photo upload or removal, or a purchase that
// changed stock. Implemented in Cashere.Server as a thin wrapper over the
// SignalR hub context; Cashere.Data's admin services only depend on this
// interface, keeping them free of any SignalR/ASP.NET Core dependency - same
// separation IPosSyncClientService keeps between Cashere and Cashere.Android.
public interface IProductCatalogChangeNotifier
{
    Task NotifyChangedAsync();
}