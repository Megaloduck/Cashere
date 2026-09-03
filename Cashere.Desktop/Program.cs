using System;
using Avalonia;
using Cashere.Data;
using Cashere.Server;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Desktop;

sealed class Program
{
    private static readonly CashereServerHost ServerHost = new();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var dbPath = CashereDbContext.GetDefaultDbPath();

        ApplyMigrations(dbPath);
        ServerHost.StartAsync(dbPath).GetAwaiter().GetResult();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            ServerHost.StopAsync().GetAwaiter().GetResult();
        }
    }

    private static void ApplyMigrations(string dbPath)
    {
        using var db = new CashereDbContext(new DbContextOptionsBuilder<CashereDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options);
        db.Database.Migrate();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
