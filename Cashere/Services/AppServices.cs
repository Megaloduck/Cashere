using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    public static class AppServices
    {
        public static IProductCatalogService? ProductCatalog { get; set; }
        public static ISaleService? SaleService { get; set; }
        public static IShopContextService? ShopContext { get; set; }
        public static IPosSyncClientService? SyncClient { get; set; }
        public static IBarcodeScannerService? BarcodeScanner { get; set; }
        public static IPhotoCaptureService? PhotoCapture { get; set; }
        public static IProductPhotoService? ProductPhoto { get; set; }
        public static IProductAdminService? ProductAdmin { get; set; }
        public static ICategoryAdminService? CategoryAdmin { get; set; }
        public static ISupplierAdminService? SupplierAdmin { get; set; }
        public static IPurchaseAdminService? PurchaseAdmin { get; set; }
        public static ICashierAdminService? CashierAdmin { get; set; }
        public static ICustomerAdminService? CustomerAdmin { get; set; }
        public static ISalesReportService? SalesReport { get; set; }
        public static IConnectedDeviceService? ConnectedDevices { get; set; }
        public static IReceiptPrinterService? ReceiptPrinter { get; set; }
        public static IShiftAdminService? ShiftAdmin { get; set; }
        public static IDataBackupService? DataBackup { get; set; }
        public static IAboutInfoService? AboutInfo { get; set; }
        public static IRefundService? RefundService { get; set; }
        public static IVoucherAdminService? VoucherAdmin { get; set; }
        public static IReportExportService? ReportExport { get; set; }
        public static IPairingQrCodeService? PairingQrCode { get; set; }
    }
}