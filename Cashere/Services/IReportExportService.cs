using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

public interface IReportExportService
{
    // Builds an .xlsx (Summary / Daily Breakdown / Sales sheets) for the
    // given range and writes it into the reports folder next to the
    // database - mirrors IDataBackupService's own "reports"/"backups"
    // folder-next-to-db pattern. Returns the full path written.
    Task<string> ExportSalesReportAsync(DateTime fromDate, DateTime toDate);
}