using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class AboutInfoService : IAboutInfoService
{
    // Bumped by hand alongside real feature milestones - there's no build
    // versioning wired into the .csproj yet, so this is the single source
    // of truth rather than something like Assembly.GetName().Version,
    // which would just show .NET's unconfigured default (1.0.0.0).
    private const string AppVersion = "1.0.0 (Preview)";

    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;
    private readonly string _dbPath;

    public AboutInfoService(IDbContextFactory<CashereDbContext> dbContextFactory, string dbPath)
    {
        _dbContextFactory = dbContextFactory;
        _dbPath = dbPath;
    }

    public async Task<AboutInfo> GetAboutInfoAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();

        var info = new FileInfo(_dbPath);

        return new AboutInfo(
            AppVersion,
            _dbPath,
            FormatBytes(info.Exists ? info.Length : 0),
            info.Exists ? info.LastWriteTimeUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm") : "Unknown",
            appliedMigrations.Count());
    }

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