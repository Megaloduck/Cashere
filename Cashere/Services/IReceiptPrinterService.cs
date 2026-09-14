using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

public record ReceiptPrintResult(bool Success, string? ErrorMessage);

// Implemented in Cashere.Desktop using System.Drawing.Printing against
// whichever printer Settings -> Hardware has configured (falling back to
// Windows' own default printer when none is set) - same "shared interface,
// platform-specific implementation" split IBarcodeScannerService and
// IPhotoCaptureService use for Android.
public interface IReceiptPrinterService
{
    Task<ReceiptPrintResult> PrintAsync(string receiptText);

    // Desktop-only capability, used by the Hardware settings screen's
    // printer picker. Returns whatever Windows currently reports as
    // installed; an empty list on any implementation that can't enumerate
    // printers.
    IReadOnlyList<string> GetInstalledPrinterNames();
}