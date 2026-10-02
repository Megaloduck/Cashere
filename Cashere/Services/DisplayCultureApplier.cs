using System.Globalization;
using System.Threading;

namespace Cashere.Services;

public static class DisplayCultureApplier
{
    private static readonly CultureInfo OperatingSystemCulture = CultureInfo.CurrentCulture;

    public static void Apply(string? cultureName)
    {
        CultureInfo culture;
        try
        {
            culture = string.IsNullOrWhiteSpace(cultureName)
                ? OperatingSystemCulture
                : CultureInfo.GetCultureInfo(cultureName.Trim());
        }
        catch (CultureNotFoundException)
        {
            culture = OperatingSystemCulture;
        }

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
