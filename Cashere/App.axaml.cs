using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
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
                AppServices.CashierAdmin is not null)
            {
                var cashier = AppServices.ShopContext.GetDefaultCashierAsync().GetAwaiter().GetResult();
                var taxRatePercent = AppServices.ShopContext.GetTaxRatePercentAsync().GetAwaiter().GetResult();

                var posViewModel = new PosViewModel(
                    AppServices.ProductCatalog,
                    AppServices.SaleService,
                    taxRatePercent,
                    cashier?.Id ?? 0,
                    cashier?.DisplayName ?? "Unknown");

                var adminViewModel = new AdminViewModel(
                    AppServices.ProductAdmin,
                    AppServices.CategoryAdmin,
                    AppServices.SupplierAdmin,
                    AppServices.PurchaseAdmin,
                    AppServices.CashierAdmin,
                    AppServices.ProductCatalog,
                    AppServices.ShopContext,
                    cashier?.Id ?? 0);

                var shell = new ShellViewModel(posViewModel, adminViewModel);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = shell
                };

                _ = posViewModel.InitializeAsync();
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
                var pairingViewModel = new PairingViewModel(AppServices.SyncClient, AppServices.BarcodeScanner);

                singleViewPlatform.MainView = new PairingView
                {
                    DataContext = pairingViewModel
                };

                _ = pairingViewModel.InitializeAsync();
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
