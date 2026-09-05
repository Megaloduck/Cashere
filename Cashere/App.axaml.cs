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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            if (AppServices.ProductCatalog is not null &&
                AppServices.SaleService is not null &&
                AppServices.ShopContext is not null)
            {
                // Blocking on these at startup is fine here - it's a couple of
                // single-row lookups against a local SQLite file before the
                // window is even shown.
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
                // Fallback for design-time / any path where AppServices wasn't
                // wired up (e.g. running Cashere.Desktop directly without the
                // usual Program.cs startup sequence).
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
            }
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
