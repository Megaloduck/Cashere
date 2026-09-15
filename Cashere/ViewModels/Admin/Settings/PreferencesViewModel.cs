using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// Only Theme is real - Language, date/time/number format, sound, and UI
// density all need infrastructure that doesn't exist yet (i18n strings, a
// locale-aware formatter used everywhere instead of hardcoded "Rp {0:N0}",
// an audio layer). Theme works because BauhausTheme.axaml's brushes were
// already built on DynamicResource Color bindings - see that file's
// ThemeDictionaries for the Light/Dark palettes, and ThemeApplier for how a
// saved choice gets applied both at startup and live when Save runs here.
public partial class PreferencesViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    public IReadOnlyList<AppThemeMode> ThemeModes { get; } = Enum.GetValues<AppThemeMode>();

    [ObservableProperty] private AppThemeMode _selectedTheme = AppThemeMode.System;
    [ObservableProperty] private string? _statusMessage;

    public PreferencesViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        SelectedTheme = settings.ThemeMode;
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();
        settings.ThemeMode = SelectedTheme;

        await _shopContext.UpdateSettingsAsync(settings);

        // Applied immediately, not just on next launch - same "live" feel
        // as every other toggle in this app.
        ThemeApplier.Apply(SelectedTheme);

        StatusMessage = "Saved.";
    }
}