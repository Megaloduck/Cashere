using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.ViewModels.Admin;

// Sample basket used purely to render the on-screen receipt preview - a real
// sale's line items are never available from this screen, so a small fixed
// basket stands in to show how the shop's name/address/phone/currency/tax
// rate/footer text will actually look once printed.
public sealed record ReceiptPreviewLine(string Name, int Quantity, decimal UnitPrice)
{
    public decimal Subtotal => Quantity * UnitPrice;
}

public partial class ReceiptAdminViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;
    private readonly IReceiptPrinterService? _receiptPrinter;

    // Captured on load so Save() can round-trip ServerBindAddress/ServerPort -
    // now owned by SyncronizationAdminViewModel - without clobbering whatever
    // was last saved there.
    private ReceiptAdmin? _loadedSettings;

    [ObservableProperty] private string _shopName = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _currency = "IDR";
    [ObservableProperty] private string _taxRatePercent = "0";
    [ObservableProperty] private string _receiptFooterText = string.Empty;
    [ObservableProperty] private string? _statusMessage;

    // Fixed sample basket for the receipt preview panel - not persisted or
    // editable, just enough line items to make the layout look like a real
    // receipt while every other value on the preview stays live-bound to
    // the fields above.
    public IReadOnlyList<ReceiptPreviewLine> PreviewLines { get; } = new List<ReceiptPreviewLine>
    {
        new("House Blend Coffee", 2, 28_000m),
        new("Butter Croissant", 1, 22_000m),
        new("Bottled Water 600ml", 3, 8_000m),
    };

    public string PreviewDateTime { get; } = DateTime.Now.ToString("dd MMM yyyy HH:mm");
    public string PreviewReceiptNumber { get; } = $"R{DateTime.Now:yyyyMMdd}-0001";
    public string PreviewReferenceNumber { get; } = $"REF-{DateTime.Now:HHmmss}";

    // Real default cashier when one exists (set in LoadAsync) - falls back to
    // a placeholder only if no cashier has been set up yet.
    [ObservableProperty] private string _previewCashierName = "Sample Cashier";

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    [ObservableProperty] private PaymentMethod _previewPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty] private bool _isPrinting;

    [ObservableProperty] private string? _printStatusMessage;

    public bool IsPreviewCashPayment => PreviewPaymentMethod == PaymentMethod.Cash;
    public bool IsPreviewNonCashPayment => !IsPreviewCashPayment;

    public decimal PreviewSubtotal => PreviewLines.Sum(l => l.Subtotal);

    private decimal ParsedPreviewTaxRate => decimal.TryParse(TaxRatePercent, out var rate) ? rate : 0;

    public decimal PreviewTaxAmount =>
        Math.Round(PreviewSubtotal * (ParsedPreviewTaxRate / 100m), 0, MidpointRounding.AwayFromZero);

    public decimal PreviewTotal => PreviewSubtotal + PreviewTaxAmount;

    // Rounded up to the nearest 5,000 so it reads like a cashier actually
    // handed over a round note rather than an implausibly exact amount.
    // Only meaningful for cash - non-cash payments always tender the exact total.
    public decimal PreviewAmountTendered => IsPreviewCashPayment
        ? RoundUpToNearest(PreviewTotal, 5_000m)
        : PreviewTotal;

    public decimal PreviewChangeDue => IsPreviewCashPayment
        ? Math.Max(0, PreviewAmountTendered - PreviewTotal)
        : 0;

    public string PreviewCurrency => string.IsNullOrWhiteSpace(Currency) ? "IDR" : Currency.Trim().ToUpperInvariant();

    public string PreviewTaxLabel => $"TAX ({ParsedPreviewTaxRate:0.##}%)";

    public string PreviewSubtotalDisplay => $"{PreviewCurrency} {PreviewSubtotal:N0}";
    public string PreviewTaxDisplay => $"{PreviewCurrency} {PreviewTaxAmount:N0}";
    public string PreviewTotalDisplay => $"{PreviewCurrency} {PreviewTotal:N0}";
    public string PreviewAmountTenderedDisplay => $"{PreviewCurrency} {PreviewAmountTendered:N0}";
    public string PreviewChangeDueDisplay => $"{PreviewCurrency} {PreviewChangeDue:N0}";

    public ReceiptAdminViewModel(IShopContextService shopContext, IReceiptPrinterService? receiptPrinter = null)
    {
        _shopContext = shopContext;
        _receiptPrinter = receiptPrinter;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is not null)
        {
            _loadedSettings = settings;

            ShopName = settings.ShopName;
            Address = settings.Address ?? string.Empty;
            Phone = settings.Phone ?? string.Empty;
            Currency = settings.Currency;
            TaxRatePercent = settings.TaxRatePercent.ToString();
            ReceiptFooterText = settings.ReceiptFooterText ?? string.Empty;
        }

        var cashier = await _shopContext.GetDefaultCashierAsync();
        PreviewCashierName = string.IsNullOrWhiteSpace(cashier?.DisplayName)
            ? "Sample Cashier"
            : cashier!.DisplayName;
    }

    partial void OnTaxRatePercentChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewTaxAmount));
        OnPropertyChanged(nameof(PreviewTotal));
        OnPropertyChanged(nameof(PreviewTaxLabel));
        OnPropertyChanged(nameof(PreviewTaxDisplay));
        OnPropertyChanged(nameof(PreviewTotalDisplay));
        OnPropertyChanged(nameof(PreviewAmountTendered));
        OnPropertyChanged(nameof(PreviewChangeDue));
        OnPropertyChanged(nameof(PreviewAmountTenderedDisplay));
        OnPropertyChanged(nameof(PreviewChangeDueDisplay));
    }

    partial void OnCurrencyChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewCurrency));
        OnPropertyChanged(nameof(PreviewSubtotalDisplay));
        OnPropertyChanged(nameof(PreviewTaxDisplay));
        OnPropertyChanged(nameof(PreviewTotalDisplay));
        OnPropertyChanged(nameof(PreviewAmountTenderedDisplay));
        OnPropertyChanged(nameof(PreviewChangeDueDisplay));
    }

    partial void OnPreviewPaymentMethodChanged(PaymentMethod value)
    {
        OnPropertyChanged(nameof(IsPreviewCashPayment));
        OnPropertyChanged(nameof(IsPreviewNonCashPayment));
        OnPropertyChanged(nameof(PreviewAmountTendered));
        OnPropertyChanged(nameof(PreviewChangeDue));
        OnPropertyChanged(nameof(PreviewAmountTenderedDisplay));
        OnPropertyChanged(nameof(PreviewChangeDueDisplay));
    }

    private static decimal RoundUpToNearest(decimal value, decimal increment)
        => increment <= 0 ? value : Math.Ceiling(value / increment) * increment;

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        if (string.IsNullOrWhiteSpace(ShopName))
        {
            StatusMessage = "Shop name is required.";
            return;
        }

        if (!decimal.TryParse(TaxRatePercent, out var taxRate))
        {
            StatusMessage = "Tax rate must be a valid number.";
            return;
        }

        var settings = _loadedSettings ?? new ReceiptAdmin();
        settings.ShopName = ShopName.Trim();
        settings.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
        settings.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        settings.Currency = Currency.Trim();
        settings.TaxRatePercent = taxRate;
        settings.ReceiptFooterText = string.IsNullOrWhiteSpace(ReceiptFooterText) ? null : ReceiptFooterText.Trim();

        await _shopContext.UpdateSettingsAsync(settings);
        _loadedSettings = settings;

        StatusMessage = "Saved.";
    }

    [RelayCommand]
    private async Task TestPrint()
    {
        if (_receiptPrinter is null)
        {
            PrintStatusMessage = "No printer service is available in this build.";
            return;
        }

        IsPrinting = true;
        PrintStatusMessage = null;
        try
        {
            var result = await _receiptPrinter.PrintAsync(BuildReceiptText());
            PrintStatusMessage = result.Success
                ? "Test receipt sent to the printer."
                : result.ErrorMessage ?? "Print failed - check the printer and try again.";
        }
        catch (Exception ex)
        {
            PrintStatusMessage = $"Print failed: {ex.Message}";
        }
        finally
        {
            IsPrinting = false;
        }
    }

    // Plain-text rendering of the same preview shown on screen - kept in one
    // place so the on-screen card and the printed output can never drift
    // out of sync with each other.
    private string BuildReceiptText()
    {
        const string divider = "----------------------------------------";
        var sb = new StringBuilder();

        sb.AppendLine(Center(ShopName));
        if (!string.IsNullOrWhiteSpace(Address)) sb.AppendLine(Center(Address));
        if (!string.IsNullOrWhiteSpace(Phone)) sb.AppendLine(Center(Phone));
        sb.AppendLine(divider);
        sb.AppendLine($"{PreviewDateTime,-20}{PreviewReceiptNumber,20}");
        sb.AppendLine($"Cashier: {PreviewCashierName}");
        sb.AppendLine(divider);

        foreach (var line in PreviewLines)
        {
            sb.AppendLine(line.Name);
            sb.AppendLine($"  {line.Quantity} x{line.Subtotal,30:N0}");
        }

        sb.AppendLine(divider);
        sb.AppendLine($"{"SUBTOTAL",-20}{PreviewSubtotalDisplay,20}");
        sb.AppendLine($"{PreviewTaxLabel,-20}{PreviewTaxDisplay,20}");
        sb.AppendLine($"{"TOTAL",-20}{PreviewTotalDisplay,20}");
        sb.AppendLine(divider);

        if (IsPreviewCashPayment)
        {
            sb.AppendLine($"{"CASH",-20}{PreviewAmountTenderedDisplay,20}");
            sb.AppendLine($"{"CHANGE",-20}{PreviewChangeDueDisplay,20}");
        }
        else
        {
            sb.AppendLine($"Paid via {PreviewPaymentMethod}");
            sb.AppendLine($"Ref: {PreviewReferenceNumber}");
        }

        if (!string.IsNullOrWhiteSpace(ReceiptFooterText))
        {
            sb.AppendLine(divider);
            sb.AppendLine(Center(ReceiptFooterText));
        }

        return sb.ToString();

        static string Center(string text)
        {
            const int width = 40;
            if (string.IsNullOrEmpty(text) || text.Length >= width) return text;
            var pad = (width - text.Length) / 2;
            return new string(' ', pad) + text;
        }
    }
}