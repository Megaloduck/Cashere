using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Cashere.ViewModels.Admin;
using Cashere.ViewModels.Pos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels;

public partial class RootViewModel : ViewModelBase
{
    private readonly IProductCatalogService _productCatalog;
    private readonly ISaleService _saleService;
    private readonly IShopContextService _shopContext;
    private readonly IProductAdminService _productAdmin;
    private readonly ICategoryAdminService _categoryAdmin;
    private readonly ISupplierAdminService _supplierAdmin;
    private readonly IPurchaseAdminService _purchaseAdmin;
    private readonly ICashierAdminService _cashierAdmin;
    private readonly ICustomerAdminService _customerAdmin;
    private readonly ISalesReportService _salesReport;
    private readonly IShiftAdminService _shiftAdmin;
    private readonly IDataBackupService _dataBackup;
    private readonly IAboutInfoService _aboutInfo;
    private readonly IConnectedDeviceService? _connectedDevices;
    private readonly IReceiptPrinterService? _receiptPrinter;
    private readonly IRefundService? _refundService;
    private readonly IVoucherAdminService? _voucherAdmin;

    [ObservableProperty]
    private ViewModelBase _currentView = null!;

    public RootViewModel(
        IProductCatalogService productCatalog,
        ISaleService saleService,
        IShopContextService shopContext,
        IProductAdminService productAdmin,
        ICategoryAdminService categoryAdmin,
        ISupplierAdminService supplierAdmin,
        IPurchaseAdminService purchaseAdmin,
        ICashierAdminService cashierAdmin,
        ICustomerAdminService customerAdmin,
        ISalesReportService salesReport,
        IShiftAdminService shiftAdmin,
        IDataBackupService dataBackup,
        IAboutInfoService aboutInfo,
        IConnectedDeviceService? connectedDevices,
        IReceiptPrinterService? receiptPrinter,
        IRefundService? refundService,
        IVoucherAdminService? voucherAdmin)
    {
        _productCatalog = productCatalog;
        _saleService = saleService;
        _shopContext = shopContext;
        _productAdmin = productAdmin;
        _categoryAdmin = categoryAdmin;
        _supplierAdmin = supplierAdmin;
        _purchaseAdmin = purchaseAdmin;
        _cashierAdmin = cashierAdmin;
        _customerAdmin = customerAdmin;
        _salesReport = salesReport;
        _shiftAdmin = shiftAdmin;
        _dataBackup = dataBackup;
        _aboutInfo = aboutInfo;
        _connectedDevices = connectedDevices;
        _receiptPrinter = receiptPrinter;
        _refundService = refundService;
        _voucherAdmin = voucherAdmin;

        AutoLockService.LockTriggered += OnAutoLockTriggered;
    }

    public Task InitializeAsync() => ShowLoginAsync();

    private Task ShowLoginAsync()
    {
        AutoLockService.Stop();

        var login = new LoginViewModel(_cashierAdmin, "Cashere");
        login.LoginSucceeded += OnLoginSucceeded;   
        CurrentView = login;

        _ = LoadShopNameAsync(login);
        return Task.CompletedTask;
    }

    private async Task LoadShopNameAsync(LoginViewModel login)
    {
        try
        {
            var shopName = await _shopContext.GetShopNameAsync();
            if (!string.IsNullOrWhiteSpace(shopName))
            {
                login.ShopName = shopName;
            }
        }
        catch
        {
        }
    }

    private async void OnLoginSucceeded(Cashier cashier)
    {
        var taxRatePercent = await _shopContext.GetTaxRatePercentAsync();

        var security = await _shopContext.GetSecuritySettingsAsync();
        AutoLockService.Configure(security.AutoLockEnabled, security.AutoLockTimeoutMinutes);

        var preferences = await _shopContext.GetSettingsAsync();
        ClockPreferenceService.Configure(preferences?.ClockSource ?? ClockSource.SystemLocal);

        var headerClock = await _shopContext.GetHeaderClockSettingsAsync();
        HeaderClockService.Current.Configure(
            headerClock.IsVisible, headerClock.ShowDay, headerClock.ShowDate,
            headerClock.ShowMonth, headerClock.ShowYear, headerClock.ShowHours);

        var posViewModel = new PosViewModel(
            _productCatalog,
            _saleService,
            _shopContext,
            taxRatePercent,
            cashier.Id,
            cashier.DisplayName,
            _customerAdmin,
            _receiptPrinter,
            _voucherAdmin);

        var adminViewModel = new AdminViewModel(
    _productAdmin, _categoryAdmin, _supplierAdmin, _purchaseAdmin, _cashierAdmin, _customerAdmin,
    _salesReport, _productCatalog, _shopContext, _shiftAdmin, _dataBackup, _aboutInfo,
    cashier.Id, cashier.Role, cashier.Username,
    _connectedDevices, _receiptPrinter, _refundService, _voucherAdmin);

        var shell = new ShellViewModel(posViewModel, adminViewModel);
        shell.LogoutRequested += OnLogoutRequested;

        CurrentView = shell;

        await posViewModel.InitializeAsync();
    }

    private async void OnLogoutRequested() => await ShowLoginAsync();

    private async void OnAutoLockTriggered() => await ShowLoginAsync();
}