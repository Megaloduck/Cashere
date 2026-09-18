using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// App-wide conversion point for displaying stored (always-UTC) timestamps.
// Static, same shape as ThemeApplier/AutoLockService - configured once at
// login and again whenever Preferences is saved, then every bound
// timestamp that goes through ToDisplay (or the converter below) agrees.
public static class ClockPreferenceService
{
    private static ClockSource _source = ClockSource.SystemLocal;

    public static void Configure(ClockSource source) => _source = source;

    public static DateTime ToDisplay(DateTime storedUtc) => _source switch
    {
        ClockSource.Utc => DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc),
        _ => storedUtc.Kind == DateTimeKind.Utc ? storedUtc.ToLocalTime() : storedUtc
    };
}