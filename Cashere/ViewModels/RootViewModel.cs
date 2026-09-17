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

// Top-level state machine for the desktop app: shows LoginView until a
// cashier authenticates, then builds and shows the Pos/Admin shell for
// that cashier - torn back down to LoginView on logout, or automatically by
// AutoLockService after Settings -> Security's inactivity timeout fires.
// Mirrors the same "own a CurrentView property, swap it on an event"
// shape ShellViewModel already uses for Pos<->Admin, just one level higher
// up the tree. MainWindow.axaml binds to CurrentView with
// x:CompileBindings="False", so it keeps working unchanged whether its
// DataContext is this class or (as before) ShellViewModel directly.
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
        IReceiptPrinterService? receiptPrinter)
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

        AutoLockService.LockTriggered += OnAutoLockTriggered;
    }

    public Task InitializeAsync() => ShowLoginAsync();

    private Task ShowLoginAsync()
    {
        // Disarm on the way to the login screen, whether that's a normal
        // logout or the lock itself firing - otherwise a still-armed timer
        // would try to "lock" a screen that's already showing Login.
        AutoLockService.Stop();

        // Shown with a placeholder shop name immediately (no async gap
        // before MainWindow has content), then patched in once the real
        // name loads - LoginViewModel.ShopName is observable, so the
        // subtitle just updates in place a moment later.
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
            // Cosmetic subtitle only - not worth surfacing a startup error over it.
        }
    }

    private async void OnLoginSucceeded(Cashier cashier)
    {
        var taxRatePercent = await _shopContext.GetTaxRatePercentAsync();

        var security = await _shopContext.GetSecuritySettingsAsync();
        AutoLockService.Configure(security.AutoLockEnabled, security.AutoLockTimeoutMinutes);

        var posViewModel = new PosViewModel(
            _productCatalog,
            _saleService,
            _shopContext,
            taxRatePercent,
            cashier.Id,
            cashier.DisplayName,
            _customerAdmin,
            _receiptPrinter);

        var adminViewModel = new AdminViewModel(
            _productAdmin,
            _categoryAdmin,
            _supplierAdmin,
            _purchaseAdmin,
            _cashierAdmin,
            _customerAdmin,
            _salesReport,
            _productCatalog,
            _shopContext,
            _shiftAdmin,
            _dataBackup,
            _aboutInfo,
            cashier.Id,
            cashier.Role,
            _connectedDevices,
            _receiptPrinter);

        var shell = new ShellViewModel(posViewModel, adminViewModel);
        shell.LogoutRequested += OnLogoutRequested;

        CurrentView = shell;

        await posViewModel.InitializeAsync();
    }

    private async void OnLogoutRequested() => await ShowLoginAsync();

    // DispatcherTimer.Tick already runs on the UI thread, so this can touch
    // CurrentView directly - no Dispatcher.UIThread.Post needed here, unlike
    // the SignalR-thread handlers elsewhere in the app (e.g.
    // DevicesAdminViewModel) that genuinely fire off-thread.
    private async void OnAutoLockTriggered() => await ShowLoginAsync();
}