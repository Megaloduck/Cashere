using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

public partial class SettingsShellViewModel : ViewModelBase
{
    public BusinessInfoViewModel Business { get; }
    public ReceiptAdminViewModel Receipts { get; }
    public PaymentSettingsViewModel Payments { get; }
    public InventorySettingsViewModel Inventory { get; }
    public HardwareSettingsViewModel Hardware { get; }  
    public SyncronizationAdminViewModel Synchronization { get; }
    public NetworkSettingsViewModel Network { get; }
    public CashRegisterViewModel CashRegister { get; }
    public SalesBehaviorSettingsViewModel SalesBehavior { get; }
    public DataBackupSettingsViewModel DataBackup { get; }
    public PreferencesViewModel Preferences { get; }
    public AboutViewModel About { get; }

    public StaffPermissionsViewModel Staff { get; }
    public SecuritySettingsViewModel Security { get; }

    // Re-raised from Staff.ManageCashiersRequested - AdminViewModel handles
    // this by flipping its own SelectedSection to AdminSection.Cashiers,
    // same bubble-it-up pattern as BackRequested/LogoutRequested elsewhere.
    public event Action? ManageCashiersRequested;

    // Re-raised from DataBackup.DatabaseWasReset - AdminViewModel forwards
    // this into its own LogoutRequested chain, since a reset deletes the
    // signed-in cashier's own row.
    public event Action? DatabaseWasReset;

    [ObservableProperty]
    private SettingsSection _selectedSection = SettingsSection.Business;

    public ViewModelBase CurrentSettingsView => SelectedSection switch
    {
        SettingsSection.Business => Business,
        SettingsSection.Receipts => Receipts,
        SettingsSection.Payments => Payments,
        SettingsSection.Inventory => Inventory,
        SettingsSection.Staff => Staff,
        SettingsSection.Hardware => Hardware,
        SettingsSection.Network => Network,
        SettingsSection.CashRegister => CashRegister,
        SettingsSection.SalesBehavior => SalesBehavior,
        SettingsSection.Security => Security,
        SettingsSection.DataBackup => DataBackup,
        SettingsSection.Preferences => Preferences,
        SettingsSection.About => About,
        _ => Business
    };

    public SettingsShellViewModel(
        IShopContextService shopContext,
        IReceiptPrinterService? receiptPrinter,
        IConnectedDeviceService? connectedDevices,
        IShiftAdminService shiftAdmin,
        IDataBackupService dataBackup,
        IAboutInfoService aboutInfo,
        ICashierAdminService cashierAdmin,
        int currentCashierId,
        UserRole currentRole,
        string currentUsername)
    {
        Business = new BusinessInfoViewModel(shopContext);
        Receipts = new ReceiptAdminViewModel(shopContext, receiptPrinter);
        Payments = new PaymentSettingsViewModel(shopContext);
        Inventory = new InventorySettingsViewModel(shopContext);
                Synchronization = new SyncronizationAdminViewModel(shopContext, AppServices.PairingQrCode);
        Hardware = new HardwareSettingsViewModel(shopContext, receiptPrinter);
        Network = new NetworkSettingsViewModel(shopContext, connectedDevices);
        CashRegister = new CashRegisterViewModel(shiftAdmin, currentCashierId);
        SalesBehavior = new SalesBehaviorSettingsViewModel(shopContext);
        DataBackup = new DataBackupSettingsViewModel(
            dataBackup, AppServices.ReportExport, cashierAdmin, currentRole, currentUsername);
        Preferences = new PreferencesViewModel(shopContext);
        About = new AboutViewModel(aboutInfo);

        Staff = new StaffPermissionsViewModel(cashierAdmin);
        Staff.ManageCashiersRequested += () => ManageCashiersRequested?.Invoke();

        Security = new SecuritySettingsViewModel(shopContext);

        DataBackup.DatabaseWasReset += () => DatabaseWasReset?.Invoke();
    }

    public async Task InitializeAsync()
    {
        await Business.LoadAsync();
        await Receipts.LoadAsync();
        await Payments.LoadAsync();
        await Inventory.LoadAsync();
        await Staff.LoadAsync();
        await Hardware.LoadAsync();
        await Network.InitializeAsync();
        await CashRegister.LoadAsync();
        await SalesBehavior.LoadAsync();
        await Security.LoadAsync();
        await DataBackup.LoadAsync();
        await Preferences.LoadAsync();
        await About.LoadAsync();
    }

    partial void OnSelectedSectionChanged(SettingsSection value)
    {
        OnPropertyChanged(nameof(CurrentSettingsView));

        if (value == SettingsSection.Network)
        {
            _ = Network.RefreshAsync();
        }
        else if (value == SettingsSection.Business)
        {
            _ = Business.LoadAsync();
        }
        else if (value == SettingsSection.Receipts)
        {
            _ = Receipts.LoadAsync();
        }
        else if (value == SettingsSection.Payments)
        {
            _ = Payments.LoadAsync();
        }
        else if (value == SettingsSection.Inventory)
        {
            _ = Inventory.LoadAsync();
        }
        else if (value == SettingsSection.Staff)
        {
            _ = Staff.LoadAsync();
        }
        else if (value == SettingsSection.Hardware)
        {
            _ = Hardware.LoadAsync();
        }
        else if (value == SettingsSection.CashRegister)
        {
            _ = CashRegister.LoadAsync();
        }
        else if (value == SettingsSection.SalesBehavior)
        {
            _ = SalesBehavior.LoadAsync();
        }
        else if (value == SettingsSection.Security)
        {
            _ = Security.LoadAsync();
        }
        else if (value == SettingsSection.DataBackup)
        {
            _ = DataBackup.LoadAsync();
        }
        else if (value == SettingsSection.Preferences)
        {
            _ = Preferences.LoadAsync();
        }
        else if (value == SettingsSection.About)
        {
            _ = About.LoadAsync();
        }
    }

    [RelayCommand]
    private void SelectSection(SettingsSection section) => SelectedSection = section;
}