using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Cashere.Models;
using Cashere.ViewModels;
using Cashere.Views;
using Cashere.Services;
using Cashere.ViewModels.Pos;
using Cashere.ViewModels.Mobile;
using Cashere.Views.Mobile;
using Cashere.ViewModels.Admin;     

namespace Cashere;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            if (AppServices.ProductCatalog is not null &&
     AppServices.SaleService is not null &&
     AppServices.ShopContext is not null &&
     AppServices.ProductAdmin is not null &&
     AppServices.CategoryAdmin is not null &&
     AppServices.SupplierAdmin is not null &&
     AppServices.PurchaseAdmin is not null &&
     AppServices.CashierAdmin is not null &&
     AppServices.CustomerAdmin is not null &&
     AppServices.SalesReport is not null &&
     AppServices.ShiftAdmin is not null &&
     AppServices.DataBackup is not null &&
     AppServices.AboutInfo is not null)
            {
                var startupSettings = AppServices.ShopContext.GetSettingsAsync().GetAwaiter().GetResult();
                ThemeApplier.Apply(startupSettings?.ThemeMode ?? AppThemeMode.System);

                var root = new RootViewModel(
                    AppServices.ProductCatalog,
                    AppServices.SaleService,
                    AppServices.ShopContext,
                    AppServices.ProductAdmin,
                    AppServices.CategoryAdmin,
                    AppServices.SupplierAdmin,
                    AppServices.PurchaseAdmin,
                    AppServices.CashierAdmin,
                    AppServices.CustomerAdmin,
                    AppServices.SalesReport,
                    AppServices.ShiftAdmin,
                    AppServices.DataBackup,
                    AppServices.AboutInfo,
                    AppServices.ConnectedDevices,
                    AppServices.ReceiptPrinter);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = root
                };

                _ = root.InitializeAsync();
            }
            else
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
            }
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            if (AppServices.SyncClient is not null)
            {
                var shell = new MobileShellViewModel(
    AppServices.SyncClient,
    AppServices.BarcodeScanner,
    AppServices.PhotoCapture,
    AppServices.ProductPhoto);

                singleViewPlatform.MainView = new MobileShellView
                {
                    DataContext = shell
                };

                _ = shell.InitializeAsync();
            }
            else
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}