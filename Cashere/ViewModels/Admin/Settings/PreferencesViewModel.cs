using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// Theme and control density are applied live. Theme works through the app's
// DynamicResource color bindings; density updates the app-level dynamic
// padding/size resources used by common controls. Number/date formatting can
// follow a selected .NET culture from the next launch; translated labels and
// event audio still need their own app-wide infrastructure.
//
// There is deliberately no configurable clock/timezone setting here -
// Cashere runs as a single local install, so every timestamp in the app
// always displays in this device's own local time (see
// ClockPreferenceService). Business Info's Timezone field is a separate,
// purely informational value (what timezone the shop is physically in)
// and doesn't affect this.
public partial class PreferencesViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    public IReadOnlyList<AppThemeMode> ThemeModes { get; } = Enum.GetValues<AppThemeMode>();
    public IReadOnlyList<UiDensity> UiDensities { get; } = Enum.GetValues<UiDensity>();
    public IReadOnlyList<DisplayCultureOption> DisplayCultures { get; } = BuildDisplayCultures();

    [ObservableProperty] private AppThemeMode _selectedTheme = AppThemeMode.System;
    [ObservableProperty] private UiDensity _selectedUiDensity = UiDensity.Comfortable;
    [ObservableProperty] private DisplayCultureOption _selectedDisplayCulture = new(string.Empty, "System default");
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _posSoundsEnabled;
    [ObservableProperty] private bool _showPosNotifications = true;
    [ObservableProperty] private bool _showHeaderClock = true;
    [ObservableProperty] private bool _showHeaderDay = true;
    [ObservableProperty] private bool _showHeaderDate = true;
    [ObservableProperty] private bool _showHeaderMonth = true;
    [ObservableProperty] private bool _showHeaderYear = true;
    [ObservableProperty] private bool _showHeaderHours = true;

    public PreferencesViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        SelectedTheme = settings.ThemeMode;
        SelectedUiDensity = settings.UiDensity;
        SelectedDisplayCulture = DisplayCultures.FirstOrDefault(option => option.Name == settings.DisplayCulture)
            ?? DisplayCultures[0];
        ShowHeaderClock = settings.ShowHeaderClock;
        ShowHeaderDay = settings.ShowHeaderDay;
        ShowHeaderDate = settings.ShowHeaderDate;
        ShowHeaderMonth = settings.ShowHeaderMonth;
        ShowHeaderYear = settings.ShowHeaderYear;
        ShowHeaderHours = settings.ShowHeaderHours;
        PosSoundsEnabled = settings.PosSoundsEnabled;
        ShowPosNotifications = settings.ShowPosNotifications;
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();
        settings.ThemeMode = SelectedTheme;
        settings.UiDensity = SelectedUiDensity;
        settings.DisplayCulture = SelectedDisplayCulture.Name;
        settings.ShowHeaderClock = ShowHeaderClock;
        settings.ShowHeaderDay = ShowHeaderDay;
        settings.ShowHeaderDate = ShowHeaderDate;
        settings.ShowHeaderMonth = ShowHeaderMonth;
        settings.ShowHeaderYear = ShowHeaderYear;
        settings.ShowHeaderHours = ShowHeaderHours;
        settings.PosSoundsEnabled = PosSoundsEnabled;
        settings.ShowPosNotifications = ShowPosNotifications;

        await _shopContext.UpdateSettingsAsync(settings);

        ThemeApplier.Apply(SelectedTheme);
        UiDensityApplier.Apply(SelectedUiDensity);
        HeaderClockService.Current.Configure(
            ShowHeaderClock, ShowHeaderDay, ShowHeaderDate, ShowHeaderMonth, ShowHeaderYear, ShowHeaderHours);

        StatusMessage = "Saved. Number and date formatting applies after restarting Cashere.";
    }

    private static IReadOnlyList<DisplayCultureOption> BuildDisplayCultures()
    {
        var options = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Where(culture => !string.IsNullOrWhiteSpace(culture.Name))
            .Select(culture => new DisplayCultureOption(culture.Name, culture.DisplayName))
            .DistinctBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        options.Insert(0, new DisplayCultureOption(string.Empty,
            $"System default ({CultureInfo.CurrentCulture.Name})"));
        return options;
    }
}

public sealed record DisplayCultureOption(string Name, string DisplayName);
