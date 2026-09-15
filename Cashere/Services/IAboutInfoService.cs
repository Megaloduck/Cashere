using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

public record AboutInfo(
    string AppVersion,
    string DatabasePath,
    string DatabaseSizeDisplay,
    string LastModifiedDisplay,
    int AppliedMigrationCount);

// Read-only diagnostic info for the About screen. Deliberately separate
// from IDataBackupService (which already exposes db path/size for its own
// status card) rather than reusing it directly - About additionally needs
// the applied-migration count, which means talking to CashereDbContext
// itself, a dependency DataBackupService doesn't have and shouldn't need.
public interface IAboutInfoService
{
    Task<AboutInfo> GetAboutInfoAsync();
}