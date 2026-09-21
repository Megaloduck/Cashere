using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// A labeled UTC offset, used only for Business Info's Timezone field - a
// reference value describing where the shop physically is (e.g. for
// printed/reference info), not something that affects how timestamps are
// displayed. Actual display always follows this device's own local clock -
// see ClockPreferenceService.
public record TimezoneOption(string Label, int UtcOffsetMinutes)
{
    public override string ToString() => Label; // lets a plain ComboBox render it with no ItemTemplate
}

public static class TimezonePresets
{
    public static IReadOnlyList<TimezoneOption> FixedOffsets { get; } = BuildFixedOffsets();

    public static TimezoneOption? FindByLabel(string? label) =>
        string.IsNullOrWhiteSpace(label) ? null : FixedOffsets.FirstOrDefault(o => o.Label == label);

    private static List<TimezoneOption> BuildFixedOffsets()
    {
        var list = new List<TimezoneOption> { new("Jakarta (GMT+7)", 7 * 60) };

        for (var offset = -12; offset <= 12; offset++)
        {
            if (offset == 7) continue; // already covered by "Jakarta (GMT+7)" above
            var sign = offset >= 0 ? "+" : "-";
            list.Add(new TimezoneOption($"GMT{sign}{Math.Abs(offset)}", offset * 60));
        }

        return list;
    }
}