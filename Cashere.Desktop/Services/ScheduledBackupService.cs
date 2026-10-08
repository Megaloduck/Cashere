using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cashere.Data;
using Cashere.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Desktop.Services;

/// <summary>Runs one configured database backup per local calendar day.</summary>
internal sealed class ScheduledBackupService : IDisposable
{
    private readonly string _dbPath;
    private readonly string _backupDirectory;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public ScheduledBackupService(string dbPath)
    {
        _dbPath = dbPath;
        _backupDirectory = Path.Combine(Path.GetDirectoryName(dbPath)!, "backups");
        _timer = new Timer(async _ => await CheckAndRunAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private async Task CheckAndRunAsync()
    {
        if (!await _runLock.WaitAsync(0)) return;
        try
        {
            var factory = new SqliteDbContextFactory(_dbPath);
            await using var db = factory.CreateDbContext();
            var settings = await db.ReceiptAdmin.AsNoTracking().FirstOrDefaultAsync();
            if (settings is null || !settings.ScheduledBackupsEnabled ||
                !TimeSpan.TryParseExact(settings.ScheduledBackupTime, "hh\\:mm", CultureInfo.InvariantCulture, out var scheduledTime) ||
                DateTime.Now.TimeOfDay < scheduledTime)
                return;

            Directory.CreateDirectory(_backupDirectory);
            var datePrefix = $"cashere-auto-backup-{DateTime.Now:yyyyMMdd}-";
            if (Directory.EnumerateFiles(_backupDirectory, $"{datePrefix}*.db").Any()) return;

            var backupService = new DataBackupService(_dbPath);
            await backupService.CreateScheduledBackupAsync(settings.AutomaticBackupRetentionCount);
        }
        catch (Exception ex)
        {
            // A temporary lock, disk-full condition, or database issue must not
            // take down the desktop app. The next minute's tick retries.
            System.Diagnostics.Trace.TraceError($"Scheduled backup failed: {ex}");
        }
        finally
        {
            _runLock.Release();
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _runLock.Dispose();
    }
}
