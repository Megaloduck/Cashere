using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Cashere.Models;

namespace Cashere.Services;

// Applies a saved theme preference to the running app. Works because every
// brush in Styles/BauhausTheme.axaml resolves its Color through
// DynamicResource against ThemeDictionaries keyed "Light"/"Dark" - setting
// RequestedThemeVariant re-resolves all of them live, app-wide, with no
// need to touch individual view files. ThemeVariant.Default means "follow
// the OS" and keeps doing so automatically if the OS theme changes while
// the app is running - Avalonia handles that, not this class.
public static class ThemeApplier
{
    public static void Apply(AppThemeMode mode)
    {
        if (Application.Current is null) return;

        Application.Current.RequestedThemeVariant = mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}