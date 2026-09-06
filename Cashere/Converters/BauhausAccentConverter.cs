using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cashere.Converters;

// Cycles an integer id through the Bauhaus primary triad (red -> yellow -> blue)
// so category chips and product-tile accent bars rotate consistently. Hex values
// are kept in sync with Styles/BauhausTheme.axaml by hand - if that palette
// changes, update this array too.
public class BauhausAccentConverter : IValueConverter
{
    private static readonly IBrush[] Palette =
    {
        new SolidColorBrush(Color.Parse("#D7263D")), // red
        new SolidColorBrush(Color.Parse("#F2B705")), // yellow
        new SolidColorBrush(Color.Parse("#1D4E89")), // blue
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var id = value switch
        {
            int i => i,
            long l => (int)l,
            _ => 0
        };

        var index = ((id % Palette.Length) + Palette.Length) % Palette.Length;
        return Palette[index];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}