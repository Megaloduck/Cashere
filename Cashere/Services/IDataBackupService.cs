using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

public record DatabaseStatusInfo(string DatabasePath, long FileSizeBytes, DateTime? LastModifiedUtc);

public record BackupFileInfo(string FileName, string FullPath, long FileSizeBytes, DateTime CreatedUtc)
{
    public string SizeDisplay => FormatBytes(FileSizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {units[unitIndex]}";
    }
}

// Local-first backup/restore for the SQLite database - Cashere has no cloud
// sync, so this is the only safety net against a corrupted or lost db file.
// Backups are created via SQLite's own online-backup API (safe to run while
// the app and its embedded sync server are both actively using the live
// connection, unlike a raw file copy which risks grabbing a half-written
// file), stored as timestamped copies under a "backups" folder next to the
// live database. Restoring requires a restart - same "changes take effect
// after restart" pattern already used for server bind address/port in
// Settings -> Network.
public interface IDataBackupService
{
    Task<DatabaseStatusInfo> GetStatusAsync();
    Task<BackupFileInfo> CreateBackupAsync();
    Task<List<BackupFileInfo>> GetBackupsAsync();
    Task RestoreFromBackupAsync(string backupFilePath);
    Task DeleteBackupAsync(string backupFilePath);
}