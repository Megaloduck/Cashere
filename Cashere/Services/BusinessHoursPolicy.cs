using System;
using System.Linq;
using Cashere.Models;

namespace Cashere.Services;

public static class BusinessHoursPolicy
{
    public static bool IsOpenAt(string? hoursJson, int utcOffsetMinutes, DateTime utcNow)
    {
        var localNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)
            .AddMinutes(utcOffsetMinutes);
        var week = BusinessHoursSerializer.Deserialize(hoursJson);
        var today = week.FirstOrDefault(day => day.Day == localNow.DayOfWeek);
        var currentTime = localNow.TimeOfDay;

        if (today is not null && IsWithin(today, currentTime)) return true;

        // An overnight schedule belongs to the day it opened. Check the
        // previous day's entry for its after-midnight portion.
        var previousDay = week.FirstOrDefault(day => day.Day == localNow.AddDays(-1).DayOfWeek);
        return previousDay is { IsClosed: false, OpenTime: not null, CloseTime: not null } &&
               previousDay.CloseTime < previousDay.OpenTime &&
               currentTime < previousDay.CloseTime;
    }

    private static bool IsWithin(BusinessDayHours day, TimeSpan currentTime)
    {
        if (day.IsClosed || day.OpenTime is not TimeSpan open || day.CloseTime is not TimeSpan close)
            return false;

        if (open == close) return true;
        return open < close
            ? currentTime >= open && currentTime < close
            : currentTime >= open || currentTime < close;
    }
}
