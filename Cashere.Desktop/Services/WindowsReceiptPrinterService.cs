#pragma warning disable CA1416 // Windows-only APIs - Cashere.Desktop only ships for Windows today.

using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Threading.Tasks;
using Cashere.Services;

namespace Cashere.Desktop.Services;

// Prints through whatever printer Windows currently has set as default -
// works out of the box for thermal printers that install as a normal
// Windows print queue (most ESC/POS receipt printers do). Renders the
// receipt as plain monospaced text sized for narrow thermal paper.
// Change PaperWidthMillimeters below if the printer uses 58mm rolls
// instead of the 80mm assumed here.
public class WindowsReceiptPrinterService : IReceiptPrinterService
{
    private const float PaperWidthMillimeters = 80f;
    private const float FontSizePoints = 9f;

    // Extra vertical breathing room between lines, as a multiplier of the
    // font's natural line height. Thermal printers tend to smudge when
    // lines sit flush against each other, so a small gap helps legibility.
    private const float LineSpacingMultiplier = 1.15f;

    public Task<ReceiptPrintResult> PrintAsync(string receiptText)
    {
        try
        {
            using var document = new PrintDocument();

            if (string.IsNullOrWhiteSpace(document.PrinterSettings.PrinterName) ||
                !document.PrinterSettings.IsValid)
            {
                return Task.FromResult(new ReceiptPrintResult(
                    false, "No default printer is configured on this PC."));
            }

            var lines = receiptText.Replace("\r\n", "\n").Split('\n');
            var lineIndex = 0;

            document.PrintPage += (_, e) =>
            {
                using var font = new Font("Consolas", FontSizePoints);

                // GetHeight(Graphics) returns float; declare explicitly so the
                // arithmetic below stays in float instead of inferring int
                // from MarginBounds.Top.
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
            var widthHundredthsInch = (int)(PaperWidthMillimeters / 25.4 * 100);

            // Height: size the page to fit the actual receipt when it's short,
            // so we don't feed a full page of blank paper on small orders.
            // Fall back to a sensible tall default when the content is long
            // (the driver will handle pagination via HasMorePages).
            var estimatedHeightInches = (lines.Length * FontSizePoints * LineSpacingMultiplier) / 72f;
            var heightHundredthsInch = Math.Max(
                200, // minimum ~2 inches so the header isn't clipped
                (int)(estimatedHeightInches * 100) + 100); // +1 inch of slack

            document.DefaultPageSettings.PaperSize =
                new PaperSize("Receipt", widthHundredthsInch, heightHundredthsInch);
            document.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);

            document.Print();
            return Task.FromResult(new ReceiptPrintResult(true, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ReceiptPrintResult(false, ex.Message));
        }
    }
}

#pragma warning restore CA1416