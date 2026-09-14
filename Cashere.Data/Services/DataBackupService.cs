using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Cashere.Services;
using Microsoft.Data.Sqlite;

namespace Cashere.Data.Services;

public class DataBackupService : IDataBackupService
{
    private readonly string _dbPath;
    private readonly string _backupsFolder;

    public DataBackupService(string dbPath)
    {
        _dbPath = dbPath;
        _backupsFolder = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
        Directory.CreateDirectory(_backupsFolder);
    }

    public Task<DatabaseStatusInfo> GetStatusAsync()
    {
        var info = new FileInfo(_dbPath);
        return Task.FromResult(new DatabaseStatusInfo(
            _dbPath,
            info.Exists ? info.Length : 0,
            info.Exists ? info.LastWriteTimeUtc : null));
    }

    public async Task<BackupFileInfo> CreateBackupAsync()
    {
        var fileName = $"cashere-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db";
        var destinationPath = Path.Combine(_backupsFolder, fileName);

        // SQLite's online-backup API, not a raw file copy - safe to run
        // while the desktop app and the embedded sync server both hold
        // short-lived connections open against the live db.
        await using var source = new SqliteConnection($"Data Source={_dbPath}");
        await source.OpenAsync();
        await using var destination = new SqliteConnection($"Data Source={destinationPath}");
        await destination.OpenAsync();
        source.BackupDatabase(destination);

        var info = new FileInfo(destinationPath);
        return new BackupFileInfo(fileName, destinationPath, info.Length, info.CreationTimeUtc);
    }

    public Task<List<BackupFileInfo>> GetBackupsAsync()
    {
        var files = Directory.Exists(_backupsFolder)
            ? Directory.GetFiles(_backupsFolder, "*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => new BackupFileInfo(f.Name, f.FullName, f.Length, f.CreationTimeUtc))
                .ToList()
            : new List<BackupFileInfo>();

        return Task.FromResult(files);
    }

    public Task RestoreFromBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
        {
            throw new AdminValidationException("Backup file was not found.");
        }

        // Release any pooled connections holding the live db file open
        // before overwriting it - without this, the copy below can fail
        // with a sharing violation on Windows even with no operation
        // actively running, since Microsoft.Data.Sqlite pools connections
        // (and their underlying file handles) between uses.
        SqliteConnection.ClearAllPools();

        File.Copy(backupFilePath, _dbPath, overwrite: true);

        return Task.CompletedTask;
    }

    public Task DeleteBackupAsync(string backupFilePath)
    {
        if (File.Exists(backupFilePath))
        {
            File.Delete(backupFilePath);
        }
        return Task.CompletedTask;
    }
}