#pragma warning disable CA1416 // Windows-only APIs - Cashere.Desktop only ships for Windows today.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
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

            document.PrintPage += (_, e) =>
            {
                using var font = new Font("Consolas", FontSizePoints);

                float lineHeight = font.GetHeight(e.Graphics) * LineSpacingMultiplier;
                float y = e.MarginBounds.Top;

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
            var estimatedHeightInches = (lines.Length * FontSizePoints * LineSpacingMultiplier) / 72f;
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
}

#pragma warning restore CA1416