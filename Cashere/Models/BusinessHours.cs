using System;
using System.Collections.Generic;
using System.Linq;

namespace Cashere.Models;

public record BusinessDayHours(DayOfWeek Day, bool IsClosed, TimeSpan? OpenTime, TimeSpan? CloseTime);

// (De)serializes ReceiptAdmin.BusinessHoursJson. Kept EF-free and in the
// shared Cashere project (not Cashere.Data) like every other plain data
// shape here, since BusinessInfoViewModel - which lives in Cashere, not
// Cashere.Data - needs to build and parse it directly.
public static class BusinessHoursSerializer
{
    // Monday-first display order, Sunday defaulted closed - the most common
    // starting point for a new shop; every day is fully editable afterward.
    public static List<BusinessDayHours> DefaultWeek() =>
        Enum.GetValues<DayOfWeek>()
            .OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d)
            .Select(d => new BusinessDayHours(
                d,
                IsClosed: d == DayOfWeek.Sunday,
                OpenTime: new TimeSpan(9, 0, 0),
                CloseTime: new TimeSpan(17, 0, 0)))
            .ToList();

    public static string Serialize(IEnumerable<BusinessDayHours> week) =>
        System.Text.Json.JsonSerializer.Serialize(week);

    public static List<BusinessDayHours> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return DefaultWeek();

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<BusinessDayHours>>(json);
            return parsed is { Count: > 0 } ? parsed : DefaultWeek();
        }
        catch
        {
            // A corrupt or old-shape JSON value shouldn't crash Business
            // Info - fall back to a sensible default week instead.
            return DefaultWeek();
        }
    }
}
