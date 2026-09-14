using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public NetworkSettingsViewModel Network { get; }
    public CashRegisterViewModel CashRegister { get; }
    public SalesBehaviorSettingsViewModel SalesBehavior { get; }
    public DataBackupSettingsViewModel DataBackup { get; }

    public SettingsPlaceholderViewModel Staff { get; }
    public SettingsPlaceholderViewModel Security { get; }
    public SettingsPlaceholderViewModel Preferences { get; }
    public SettingsPlaceholderViewModel About { get; }

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
        int currentCashierId)
    {
        Business = new BusinessInfoViewModel(shopContext);
        Receipts = new ReceiptAdminViewModel(shopContext, receiptPrinter);
        Payments = new PaymentSettingsViewModel(shopContext);
        Inventory = new InventorySettingsViewModel(shopContext);
        Hardware = new HardwareSettingsViewModel(shopContext, receiptPrinter);
        Network = new NetworkSettingsViewModel(shopContext, connectedDevices);
        CashRegister = new CashRegisterViewModel(shiftAdmin, currentCashierId);
        SalesBehavior = new SalesBehaviorSettingsViewModel(shopContext);
        DataBackup = new DataBackupSettingsViewModel(dataBackup);

        Staff = new SettingsPlaceholderViewModel(
            "Staff & Permissions",
            "Cashiers already have a Role (Owner/Manager/Cashier), and passwords are now hashed with PBKDF2 instead of stored as plaintext. There's still no login screen - the app auto-picks the first active cashier the same way it always has - and nothing checks Role yet.",
            new[]
            {
                "Login screen (gate the POS shell on real sign-in)",
                "Void sale / refund",
                "Apply discount / change price",
                "Open cash drawer",
                "View profit / reports",
                "Edit inventory, delete products",
                "Change settings"
            });

        Security = new SettingsPlaceholderViewModel(
            "Security",
            "No PIN, lock screen, or manager-approval gate exists yet - cashier login itself is still a placeholder (see Staff & Permissions), so there's nothing yet for these controls to attach to.",
            new[]
            {
                "PIN requirement",
                "Auto-lock timeout",
                "Manager authorization for sensitive actions",
                "Audit log",
                "Local database encryption"
            });

        Preferences = new SettingsPlaceholderViewModel(
            "Preferences",
            "App-level look and feel, separate from business configuration. Not started yet.",
            new[]
            {
                "Light / Dark / System theme",
                "Language",
                "Date, time & number format",
                "Sound & notifications",
                "UI density"
            });

        About = new SettingsPlaceholderViewModel(
            "About",
            "Cashere Point of Sale — 1.0.0 (Preview).",
            new[]
            {
                "Version",
                "Database status",
                "Licenses"
            });
    }

    public async Task InitializeAsync()
    {
        await Business.LoadAsync();
        await Receipts.LoadAsync();
        await Payments.LoadAsync();
        await Inventory.LoadAsync();
        await Hardware.LoadAsync();
        await Network.InitializeAsync();
        await CashRegister.LoadAsync();
        await SalesBehavior.LoadAsync();
        await DataBackup.LoadAsync();
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
        else if (value == SettingsSection.DataBackup)
        {
            _ = DataBackup.LoadAsync();
        }
    }

    [RelayCommand]
    private void SelectSection(SettingsSection section) => SelectedSection = section;
}