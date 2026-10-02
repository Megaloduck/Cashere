using Avalonia;
using Cashere.Models;

namespace Cashere.Services;

// Changes the app-level dynamic resources used by common controls. Keeping
// the values in one place means a saved density choice updates open screens
// immediately without rewriting each view's layout.
public static class UiDensityApplier
{
    public static void Apply(UiDensity density)
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        var compact = density == UiDensity.Compact;
        resources["TextInputPadding"] = compact ? new Thickness(8, 5) : new Thickness(10, 8);
        resources["TextInputFontSize"] = compact ? 12d : 13d;
        resources["ButtonControlPadding"] = compact ? new Thickness(12, 6) : new Thickness(16, 10);
        resources["ChoiceControlPadding"] = compact ? new Thickness(8, 5) : new Thickness(10, 8);
        resources["ChoiceItemPadding"] = compact ? new Thickness(8, 5) : new Thickness(10, 8);
        resources["DatePickerControlHeight"] = compact ? 31d : 35d;
    }
}
