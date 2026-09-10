using System;
using Avalonia;
using Cashere.Data;
using Cashere.Server;
using Microsoft.EntityFrameworkCore;
using Cashere.Data.Services;
using Cashere.Services;
using System.Linq;

namespace Cashere.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var dbPath = CashereDbContext.GetDefaultDbPath();

        ApplyMigrationsAndSeed(dbPath);

        var (port, bindAddress) = LoadNetworkSettings(dbPath);
        var serverHost = new CashereServerHost(port, bindAddress);

        try
        {
            serverHost.StartAsync(dbPath).GetAwaiter().GetResult();
        }
        catch
        {
            // The saved bind address/port may no longer be valid - moved to
            // a different network, another process already holds the port,
            // etc. Fall back to the safe defaults rather than let a bad
            // network setting brick the app on every future launch; the
            // admin can fix the setting again from the Devices/Settings
            // screen once the app is up.
            serverHost = new CashereServerHost();
            serverHost.StartAsync(dbPath).GetAwaiter().GetResult();
        }

        var dbContextFactory = new SqliteDbContextFactory(dbPath);
        AppServices.ProductCatalog = new ProductCatalogService(dbContextFactory);
        AppServices.SaleService = new SaleService(dbContextFactory);
        AppServices.ShopContext = new ShopContextService(dbContextFactory);
        AppServices.ProductAdmin = new ProductAdminService(dbContextFactory, serverHost.CatalogChangeNotifier);
        AppServices.CategoryAdmin = new CategoryAdminService(dbContextFactory);
        AppServices.SupplierAdmin = new SupplierAdminService(dbContextFactory);
        AppServices.PurchaseAdmin = new PurchaseAdminService(dbContextFactory, serverHost.CatalogChangeNotifier);
        AppServices.CashierAdmin = new CashierAdminService(dbContextFactory);
        AppServices.CustomerAdmin = new CustomerAdminService(dbContextFactory);
        AppServices.SalesReport = new SalesReportService(dbContextFactory);
        AppServices.ConnectedDevices = serverHost.ConnectedDevices;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            serverHost.StopAsync().GetAwaiter().GetResult();
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

    private static (int Port, string BindAddress) LoadNetworkSettings(string dbPath)
    {
        using var db = new CashereDbContext(new DbContextOptionsBuilder<CashereDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options);

        var settings = db.ShopSettings.AsNoTracking().FirstOrDefault();
        return (settings?.ServerPort ?? 5177, settings?.ServerBindAddress ?? "0.0.0.0");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}