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

    [STAThread]
    public static void Main(string[] args)
    {
        var dbPath = CashereDbContext.GetDefaultDbPath();

        ApplyMigrationsAndSeed(dbPath);

        // Started before AppServices are wired up below so ProductAdmin and
        // PurchaseAdmin can be constructed with ServerHost.CatalogChangeNotifier -
        // the bridge that lets admin-panel writes (built here, outside the
        // server's own DI container) push ProductCatalogChanged to connected
        // mobile clients through the same SignalR hub.
        ServerHost.StartAsync(dbPath).GetAwaiter().GetResult();

        var dbContextFactory = new SqliteDbContextFactory(dbPath);
        AppServices.ProductCatalog = new ProductCatalogService(dbContextFactory);
        AppServices.SaleService = new SaleService(dbContextFactory);
        AppServices.ShopContext = new ShopContextService(dbContextFactory);
        AppServices.ProductAdmin = new ProductAdminService(dbContextFactory, ServerHost.CatalogChangeNotifier);
        AppServices.CategoryAdmin = new CategoryAdminService(dbContextFactory);
        AppServices.SupplierAdmin = new SupplierAdminService(dbContextFactory);
        AppServices.PurchaseAdmin = new PurchaseAdminService(dbContextFactory, ServerHost.CatalogChangeNotifier);
        AppServices.CashierAdmin = new CashierAdminService(dbContextFactory);
        AppServices.CustomerAdmin = new CustomerAdminService(dbContextFactory);
        AppServices.SalesReport = new SalesReportService(dbContextFactory);
        AppServices.ConnectedDevices = ServerHost.ConnectedDevices;

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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}