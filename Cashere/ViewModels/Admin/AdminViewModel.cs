using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using Cashere.ViewModels.Admin.Settings;
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

    // Hub for Receipts, Payments, Inventory, Staff, Hardware, Network
    // (Synchronization + Connected Devices), Cash Register, Sales Behavior,
    // Security, Data & Backup, Preferences and About - see
    // ViewModels/Admin/Settings/SettingsShellViewModel.
    public SettingsShellViewModel Settings { get; }

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
        AdminSection.Settings => "SETTINGS",
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
        Settings = new SettingsShellViewModel(shopContext, receiptPrinter, connectedDeviceService);
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
        await Settings.InitializeAsync();
    }

    partial void OnSelectedSectionChanged(AdminSection value)
    {
        OnPropertyChanged(nameof(CurrentSectionViewModel));
        OnPropertyChanged(nameof(CurrentSectionTitle));
    }

    [RelayCommand]
    private void SelectSection(AdminSection section) => SelectedSection = section;

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();
}