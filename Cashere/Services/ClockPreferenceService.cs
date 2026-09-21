using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// Single conversion point for displaying stored (always-UTC) timestamps.
// Cashere runs as a single local install (one till, one machine), so
// display always follows this device's own local time zone - no
// per-install configuration needed. Every screen that shows a stored
// timestamp - the header clock, Cash Register, Sales History, Sales
// Reports, Purchases, Connected Devices - routes through this one method
// (directly, or via UtcToDisplayTimeConverter in XAML), so they can never
// disagree with each other the way the header clock and shift times used
// to.
public static class ClockPreferenceService
{
    public static DateTime ToDisplay(DateTime storedUtc)
    {
        // SQLite/EF Core always hands DateTime values back with
        // Kind = Unspecified, even though every write in this app used
        // DateTime.UtcNow - Kind can't be trusted after a DB round trip.
        // Every stored timestamp in this app IS UTC, so treat it as such
        // unconditionally before converting to local time.
        var utc = DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc);
        return utc.ToLocalTime();
    }
}