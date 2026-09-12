using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public DevicesAdminViewModel Devices { get; }
    public SyncronizationAdminViewModel Syncronization { get; }

    public event Action? BackRequested;

    [ObservableProperty]
    private AdminSection _selectedSection = AdminSection.Products;

    public ViewModelBase CurrentSectionViewModel => SelectedSection switch
    {
        AdminSection.Products => Products,
        AdminSection.Purchases => Purchases,
        AdminSection.Suppliers => Suppliers,
        AdminSection.Customers => Customers,
        AdminSection.Cashiers => Cashiers,
        AdminSection.SalesHistory => SalesHistory,
        AdminSection.SalesReport => SalesReport,
        AdminSection.Settings => Settings,
        AdminSection.Devices => Devices,
        AdminSection.Syncronization => Syncronization,
        _ => Products
    };

    public string CurrentSectionTitle => SelectedSection switch
    {
        AdminSection.Products => "PRODUCTS",
        AdminSection.Purchases => "PURCHASES",
        AdminSection.Suppliers => "SUPPLIERS",
        AdminSection.Customers => "CUSTOMERS",
        AdminSection.Cashiers => "CASHIERS",
        AdminSection.SalesHistory => "SALES HISTORY",
        AdminSection.SalesReport => "REPORTS",
        AdminSection.Settings => "SHOP SETTINGS",
        AdminSection.Devices => "CONNECTED DEVICES",
        AdminSection.Syncronization => "SYNCHRONIZATION",
        _ => "ADMIN"
    };

    public AdminViewModel(
        IProductAdminService productAdmin,
        ICategoryAdminService categoryAdmin,
        ISupplierAdminService supplierAdmin,
        IPurchaseAdminService purchaseAdmin,
        ICashierAdminService cashierAdmin,
        ICustomerAdminService customerAdmin,
        ISalesReportService salesReport,
        IProductCatalogService productCatalog,
        IShopContextService shopContext, int currentCashierId,
        IConnectedDeviceService? connectedDeviceService = null,
        IReceiptPrinterService? receiptPrinter = null)
    {
        Products = new ProductAdminViewModel(productAdmin, categoryAdmin);
        Suppliers = new SupplierAdminViewModel(supplierAdmin);
        Purchases = new PurchaseAdminViewModel(purchaseAdmin, supplierAdmin, productCatalog, currentCashierId);
        Cashiers = new CashierAdminViewModel(cashierAdmin);
        Customers = new CustomerAdminViewModel(customerAdmin);
        SalesHistory = new SalesHistoryViewModel(salesReport);
        SalesReport = new SalesReportViewModel(salesReport);
        Settings = new ShopSettingsViewModel(shopContext, receiptPrinter);
        Devices = new DevicesAdminViewModel(connectedDeviceService);
        Syncronization = new SyncronizationAdminViewModel(shopContext);
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
        await Devices.LoadAsync();
        await Syncronization.LoadAsync();
    }

    partial void OnSelectedSectionChanged(AdminSection value)
    {
        OnPropertyChanged(nameof(CurrentSectionViewModel));
        OnPropertyChanged(nameof(CurrentSectionTitle));

        if (value == AdminSection.Devices)
        {
            Devices.RefreshCommand.Execute(null);
        }
        else if (value == AdminSection.Syncronization)
        {
            _ = Syncronization.LoadAsync();
        }
    }

    [RelayCommand]
    private void SelectSection(AdminSection section) => SelectedSection = section;

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();
}