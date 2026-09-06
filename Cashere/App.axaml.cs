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
                AppServices.ShopContext is not null)
            {
                var cashier = AppServices.ShopContext.GetDefaultCashierAsync().GetAwaiter().GetResult();
                var taxRatePercent = AppServices.ShopContext.GetTaxRatePercentAsync().GetAwaiter().GetResult();

                var posViewModel = new PosViewModel(
                    AppServices.ProductCatalog,
                    AppServices.SaleService,
                    taxRatePercent,
                    cashier?.Id ?? 0,
                    cashier?.DisplayName ?? "Unknown");

                desktop.MainWindow = new MainWindow
                {
                    DataContext = posViewModel
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
                // Fallback for design-time / any path where AppServices wasn't
                // wired up (e.g. running the single-view target directly).
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
