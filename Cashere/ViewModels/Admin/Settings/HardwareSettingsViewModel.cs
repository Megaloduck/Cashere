using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// Printer selection and paper width - the only Hardware category with a
// real device behind it today (WindowsReceiptPrinterService). Scanner
// config, cash drawer, customer display and EDC pairing have no
// implementation to configure yet, so they stay a "coming soon" note on
// this same screen rather than getting their own controls.
public partial class HardwareSettingsViewModel : ViewModelBase
{
    private const string SystemDefaultLabel = "SYSTEM DEFAULT";

    private readonly IShopContextService _shopContext;
    private readonly IReceiptPrinterService? _receiptPrinter;

    // First entry is always SystemDefaultLabel so the picker can express
    // "don't override Windows' own default printer" - the behavior this
    // app always had before printer selection existed.
    public ObservableCollection<string> AvailablePrinters { get; } = new();

    public IReadOnlyList<int> PaperWidths { get; } = new[] { 58, 80 };

    [ObservableProperty] private string _selectedPrinterName = SystemDefaultLabel;
    [ObservableProperty] private int _paperWidthMm = 80;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isTestPrinting;
    [ObservableProperty] private string? _testPrintStatusMessage;
    [ObservableProperty] private bool _isPrinterServiceAvailable;

    public HardwareSettingsViewModel(IShopContextService shopContext, IReceiptPrinterService? receiptPrinter)
    {
        _shopContext = shopContext;
        _receiptPrinter = receiptPrinter;
        IsPrinterServiceAvailable = receiptPrinter is not null;
    }

    public async Task LoadAsync()
    {
        AvailablePrinters.Clear();
        AvailablePrinters.Add(SystemDefaultLabel);

        if (_receiptPrinter is not null)
        {
            foreach (var name in _receiptPrinter.GetInstalledPrinterNames())
            {
                AvailablePrinters.Add(name);
            }
        }

        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        SelectedPrinterName = string.IsNullOrWhiteSpace(settings.PrinterName)
            ? SystemDefaultLabel
            : settings.PrinterName;

        // A previously-saved printer that's no longer installed (unplugged,
        // renamed, driver removed) still gets listed so opening this screen
        // never silently wipes the saved setting just by visiting it.
        if (!AvailablePrinters.Contains(SelectedPrinterName))
        {
            AvailablePrinters.Add(SelectedPrinterName);
        }

        PaperWidthMm = settings.PrinterPaperWidthMm > 0 ? settings.PrinterPaperWidthMm : 80;
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.PrinterName = SelectedPrinterName == SystemDefaultLabel ? null : SelectedPrinterName;
        settings.PrinterPaperWidthMm = PaperWidthMm;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }

    [RelayCommand]
    private async Task TestPrint()
    {
        if (_receiptPrinter is null)
        {
            TestPrintStatusMessage = "No printer service is available in this build.";
            return;
        }

        IsTestPrinting = true;
        TestPrintStatusMessage = null;
        try
        {
            // Persist first - PrintAsync reads printer config fresh from the
            // database, so without this a test print would silently use
            // whatever was last saved instead of what's showing on screen.
            await Save();

            var text =
                "CASHERE HARDWARE TEST\n" +
                $"Printer: {SelectedPrinterName}\n" +
                $"Paper width: {PaperWidthMm}mm\n" +
                $"Printed: {DateTime.Now:dd MMM yyyy HH:mm:ss}\n";

            var result = await _receiptPrinter.PrintAsync(text);
            TestPrintStatusMessage = result.Success
                ? "Test print sent."
                : result.ErrorMessage ?? "Print failed - check the printer and try again.";
        }
        catch (Exception ex)
        {
            TestPrintStatusMessage = $"Print failed: {ex.Message}";
        }
        finally
        {
            IsTestPrinting = false;
        }
    }
}