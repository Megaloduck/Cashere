using System;
using Avalonia;
using Cashere.Data;
using Cashere.Server;
using Microsoft.EntityFrameworkCore;
using Cashere.Data.Services;
using Cashere.Services;

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

        ApplyMigrationsAndSeed(dbPath);

        var dbContextFactory = new SqliteDbContextFactory(dbPath);
        AppServices.ProductCatalog = new ProductCatalogService(dbContextFactory);
        AppServices.SaleService = new SaleService(dbContextFactory);
        AppServices.ShopContext = new ShopContextService(dbContextFactory);

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

    private static void ApplyMigrationsAndSeed(string dbPath)
    {
        using var db = new CashereDbContext(new DbContextOptionsBuilder<CashereDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options);
        db.Database.Migrate();
        SeedData.EnsureSeedDataAsync(db).GetAwaiter().GetResult();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
