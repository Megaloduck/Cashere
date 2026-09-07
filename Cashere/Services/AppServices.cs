using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    // Minimal static service locator, wired up once before the Avalonia
    // lifetime starts - in Cashere.Desktop/Program.cs for desktop, in
    // Cashere.Android/MainActivity.cs for Android.
    public static class AppServices
    {
        public static IProductCatalogService? ProductCatalog { get; set; }
        public static ISaleService? SaleService { get; set; }
        public static IShopContextService? ShopContext { get; set; }
        public static IPosSyncClientService? SyncClient { get; set; }
        public static IBarcodeScannerService? BarcodeScanner { get; set; }
        public static IProductAdminService? ProductAdmin { get; set; }
        public static ICategoryAdminService? CategoryAdmin { get; set; }
        public static ISupplierAdminService? SupplierAdmin { get; set; }
        public static IPurchaseAdminService? PurchaseAdmin { get; set; }
        public static ICashierAdminService? CashierAdmin { get; set; }
    }
}