using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin;

public partial class AdminViewModel : ViewModelBase
{
    public ProductAdminViewModel Products { get; }
    public SupplierAdminViewModel Suppliers { get; }
    public PurchaseAdminViewModel Purchases { get; }
    public CashierAdminViewModel Cashiers { get; }
    public CustomerAdminViewModel Customers { get; }
    public SalesHistoryViewModel SalesHistory { get; }
    public SalesReportViewModel SalesReport { get; }
    public ShopSettingsViewModel Settings { get; }

    public event Action? BackRequested;

    public AdminViewModel(
        IProductAdminService productAdmin,
        ICategoryAdminService categoryAdmin,
        ISupplierAdminService supplierAdmin,
        IPurchaseAdminService purchaseAdmin,
        ICashierAdminService cashierAdmin,
        ICustomerAdminService customerAdmin,
        ISalesReportService salesReport,
        IProductCatalogService productCatalog,
        IShopContextService shopContext,
        int currentCashierId)
    {
        Products = new ProductAdminViewModel(productAdmin, categoryAdmin);
        Suppliers = new SupplierAdminViewModel(supplierAdmin);
        Purchases = new PurchaseAdminViewModel(purchaseAdmin, supplierAdmin, productCatalog, currentCashierId);
        Cashiers = new CashierAdminViewModel(cashierAdmin);
        Customers = new CustomerAdminViewModel(customerAdmin);
        SalesHistory = new SalesHistoryViewModel(salesReport);
        SalesReport = new SalesReportViewModel(salesReport);
        Settings = new ShopSettingsViewModel(shopContext);
    }

    public async Task InitializeAsync()
    {
        await Products.LoadAsync();
        await Suppliers.LoadAsync();
        await Purchases.LoadAsync();
        await Cashiers.LoadAsync();
        await Customers.LoadAsync();
        await SalesHistory.LoadAsync();
        await SalesReport.LoadAsync();
        await Settings.LoadAsync();
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();
}