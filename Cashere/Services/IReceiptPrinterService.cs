using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

public record ReceiptPrintResult(bool Success, string? ErrorMessage);

// Implemented in Cashere.Desktop using System.Drawing.Printing against
// whichever printer Windows has set as default - same "shared interface,
// platform-specific implementation" split IBarcodeScannerService and
// IPhotoCaptureService use for Android. Deliberately small: today it just
// prints plain text for a sanity-check test print, but the real checkout
// receipt (and any future cash-drawer-kick command) can grow behind this
// same interface later instead of starting from scratch.
public interface IReceiptPrinterService
{
    Task<ReceiptPrintResult> PrintAsync(string receiptText);
}