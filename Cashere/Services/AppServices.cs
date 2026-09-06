using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    // Minimal static service locator, wired up once in Cashere.Desktop/Program.cs
    // before the Avalonia lifetime starts. Kept intentionally lightweight rather
    // than pulling in a full DI container - revisit once the service count grows
    // past a handful (e.g. once cashier login / shop settings editing land).
    public static class AppServices
    {
        public static IProductCatalogService? ProductCatalog { get; set; }
        public static ISaleService? SaleService { get; set; }
        public static IShopContextService? ShopContext { get; set; }
        public static IPosSyncClientService? SyncClient { get; set; }
    }

}
