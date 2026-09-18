using Avalonia.Data.Converters;
using Cashere.Services;
using System;
using System.Globalization;

namespace Cashere.Converters;

// Drop-in replacement for binding straight to a stored UTC DateTime -
// routes it through ClockPreferenceService first so every screen using
// this converter honors Settings -> Preferences -> Clock.
public class UtcToDisplayTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            DateTime dt => ClockPreferenceService.ToDisplay(dt),
            _ => value
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}