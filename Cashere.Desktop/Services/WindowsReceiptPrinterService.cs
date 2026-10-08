#pragma warning disable CA1416 // Windows-only APIs - Cashere.Desktop only ships for Windows today.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cashere.Converters;
using Cashere.Services;

namespace Cashere.Desktop.Services;

// Prints through the printer configured in Settings -> Hardware, falling
// back to whatever printer Windows currently has set as default when none
// is configured (PrinterName null/blank) or the configured one is no
// longer installed. Renders the receipt as plain monospaced text sized for
// narrow thermal paper - PaperWidthMillimeters now comes from Settings ->
// Hardware too, instead of the fixed 80mm this used to assume.
public class WindowsReceiptPrinterService : IReceiptPrinterService
{
    private const float FontSizePoints = 9f;

    // Extra vertical breathing room between lines, as a multiplier of the
    // font's natural line height. Thermal printers tend to smudge when
    // lines sit flush against each other, so a small gap helps legibility.
    private const float LineSpacingMultiplier = 1.15f;

    private readonly IShopContextService _shopContext;

    public bool SupportsCashDrawerKick => true;

    public WindowsReceiptPrinterService(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public IReadOnlyList<string> GetInstalledPrinterNames()
    {
        return PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(n => n).ToList();
    }

    public async Task<ReceiptPrintResult> PrintAsync(string receiptText)
    {
        try
        {
            var settings = await _shopContext.GetSettingsAsync();
            var paperWidthMillimeters =
                settings is not null && settings.PrinterPaperWidthMm > 0 ? settings.PrinterPaperWidthMm : 80;
            var printerName = settings?.PrinterName;
            using var logo = LoadLogo(settings?.LogoPath);

            using var document = new PrintDocument();

            if (!string.IsNullOrWhiteSpace(printerName))
            {
                document.PrinterSettings.PrinterName = printerName;
            }

            if (string.IsNullOrWhiteSpace(document.PrinterSettings.PrinterName) ||
                !document.PrinterSettings.IsValid)
            {
                return new ReceiptPrintResult(
                    false, "No valid printer is configured - check Settings -> Hardware or set a Windows default printer.");
            }

            var lines = receiptText.Replace("\r\n", "\n").Split('\n');
            var lineIndex = 0;
            var logoPrinted = false;

            document.PrintPage += (_, e) =>
            {
                using var font = new Font("Consolas", FontSizePoints);

                float lineHeight = font.GetHeight(e.Graphics) * LineSpacingMultiplier;
                float y = e.MarginBounds.Top;

                if (!logoPrinted && logo is not null)
                {
                    var maxLogoWidth = e.MarginBounds.Width * 0.8f;
                    const float maxLogoHeight = 75f;
                    var scale = Math.Min(maxLogoWidth / logo.Width, maxLogoHeight / logo.Height);
                    var logoWidth = logo.Width * scale;
                    var logoHeight = logo.Height * scale;
                    var logoX = e.MarginBounds.Left + (e.MarginBounds.Width - logoWidth) / 2f;
                    e.Graphics.DrawImage(logo, logoX, y, logoWidth, logoHeight);
                    y += logoHeight + lineHeight;
                    logoPrinted = true;
                }

                while (lineIndex < lines.Length && y + lineHeight <= e.MarginBounds.Bottom)
                {
                    e.Graphics.DrawString(
                        lines[lineIndex],
                        font,
                        Brushes.Black,
                        e.MarginBounds.Left,
                        y);

                    y += lineHeight;
                    lineIndex++;
                }

                e.HasMorePages = lineIndex < lines.Length;
            };

            // Narrow custom page size roughly matching common thermal roll
            // widths. PaperSize's unit is hundredths of an inch.
            var widthHundredthsInch = (int)(paperWidthMillimeters / 25.4 * 100);

            // Height: size the page to fit the actual receipt when it's short,
            // so we don't feed a full page of blank paper on small orders.
            var estimatedHeightInches = (lines.Length * FontSizePoints * LineSpacingMultiplier) / 72f +
                (logo is null ? 0 : 1.1f);
            var heightHundredthsInch = Math.Max(
                200, // minimum ~2 inches so the header isn't clipped
                (int)(estimatedHeightInches * 100) + 100); // +1 inch of slack

            document.DefaultPageSettings.PaperSize =
                new PaperSize("Receipt", widthHundredthsInch, heightHundredthsInch);
            document.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

            document.Print();
            return new ReceiptPrintResult(true, null);
        }
        catch (Exception ex)
        {
            return new ReceiptPrintResult(false, ex.Message);
        }
    }

    public async Task<ReceiptPrintResult> OpenCashDrawerAsync()
    {
        try
        {
            var settings = await _shopContext.GetSettingsAsync();
            var printerSettings = new PrinterSettings();
            if (!string.IsNullOrWhiteSpace(settings?.PrinterName))
                printerSettings.PrinterName = settings.PrinterName;
            if (!printerSettings.IsValid)
                return new ReceiptPrintResult(false, "The configured receipt printer is unavailable.");

            return await Task.Run(() => SendEscPosDrawerPulse(printerSettings.PrinterName));
        }
        catch (Exception ex)
        {
            return new ReceiptPrintResult(false, ex.Message);
        }
    }

    private static ReceiptPrintResult SendEscPosDrawerPulse(string printerName)
    {
        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
            return Win32Failure("Could not open the receipt printer");
        try
        {
            var documentInfo = new DocInfo1 { DocumentName = "Cashere cash drawer kick", DataType = "RAW" };
            if (StartDocPrinter(printerHandle, 1, documentInfo) == 0)
                return Win32Failure("Could not start a raw printer job");
            try
            {
                if (!StartPagePrinter(printerHandle)) return Win32Failure("Could not start the printer page");
                try
                {
                    // Standard ESC/POS pulse: drawer pin 2, 50 ms on, 250 ms off.
                    // Only sent after an Owner enables it for a compatible printer/drawer.
                    byte[] pulse = { 0x1B, 0x70, 0x00, 0x19, 0xFA };
                    if (!WritePrinter(printerHandle, pulse, pulse.Length, out var written) || written != pulse.Length)
                        return Win32Failure("The printer did not accept the drawer pulse");
                    return new ReceiptPrintResult(true, null);
                }
                finally { EndPagePrinter(printerHandle); }
            }
            finally { EndDocPrinter(printerHandle); }
        }
        finally { ClosePrinter(printerHandle); }
    }

    private static ReceiptPrintResult Win32Failure(string action) =>
        new(false, $"{action}: {Marshal.GetLastWin32Error()}.");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class DocInfo1
    {
        [MarshalAs(UnmanagedType.LPTStr)] public string DocumentName = string.Empty;
        [MarshalAs(UnmanagedType.LPTStr)] public string? OutputFile;
        [MarshalAs(UnmanagedType.LPTStr)] public string DataType = string.Empty;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinter", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);
    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printerHandle);
    [DllImport("winspool.drv", EntryPoint = "StartDocPrinter", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int StartDocPrinter(IntPtr printerHandle, int level, [In] DocInfo1 documentInfo);
    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr printerHandle);
    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr printerHandle);
    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr printerHandle);
    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr printerHandle, byte[] buffer, int count, out int written);

    private static Image? LoadLogo(string? relativeLogoPath)
    {
        if (string.IsNullOrWhiteSpace(relativeLogoPath)) return null;

        try
        {
            var fullPath = LogoPathToImageConverter.GetFullPath(relativeLogoPath);
            return System.IO.File.Exists(fullPath) ? Image.FromFile(fullPath) : null;
        }
        catch
        {
            // An unsupported/corrupt logo must never prevent the text receipt
            // itself from printing.
            return null;
        }
    }
}

#pragma warning restore CA1416
