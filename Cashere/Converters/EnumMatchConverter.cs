using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Cashere.Converters;

// Lets XAML toggle a Classes.xxx pseudo-state by comparing a bound enum
// against a ConverterParameter - used to highlight the active sidebar item
// without an IsXSelected bool per section.
public class EnumMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}