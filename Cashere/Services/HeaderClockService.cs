using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.Services;

// Live day/date/month/year/hours display for the desktop Pos/Admin shell
// headers - each component's visibility is configured under Settings ->
// Preferences and applied immediately, same "live" feel as
// ThemeApplier/AutoLockService. A DispatcherTimer ticking every second
// keeps HoursText current. "Now" is always derived from DateTime.UtcNow
// through ClockPreferenceService.ToDisplay - the same conversion every
// other stored timestamp in the app goes through (see
// UtcToDisplayTimeConverter) - which is what keeps the header clock, the
// Cash Register "Shift Open" line, and Recent Shifts all agreeing with
// each other.
public partial class HeaderClockViewModel : ObservableObject
{
    private readonly DispatcherTimer _timer;

    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _showDay = true;
    [ObservableProperty] private bool _showDate = true;
    [ObservableProperty] private bool _showMonth = true;
    [ObservableProperty] private bool _showYear = true;
    [ObservableProperty] private bool _showHours = true;

    [ObservableProperty] private string _dayText = string.Empty;
    [ObservableProperty] private string _dateText = string.Empty;
    [ObservableProperty] private string _monthText = string.Empty;
    [ObservableProperty] private string _yearText = string.Empty;
    [ObservableProperty] private string _hoursText = string.Empty;

    public HeaderClockViewModel()
    {
        UpdateNow();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateNow();
        _timer.Start();
    }

    // Called from PreferencesViewModel.Save() for an immediate effect, and
    // from RootViewModel right after login to arm it with whatever's
    // currently saved - same two call sites AutoLockService.Configure has.
    public void Configure(
        bool isVisible, bool showDay, bool showDate, bool showMonth, bool showYear, bool showHours)
    {
        IsVisible = isVisible;
        ShowDay = showDay;
        ShowDate = showDate;
        ShowMonth = showMonth;
        ShowYear = showYear;
        ShowHours = showHours;
    }

    private void UpdateNow()
    {
        var now = ClockPreferenceService.ToDisplay(DateTime.UtcNow);
        DayText = now.ToString("dddd");
        DateText = now.ToString("dd");
        MonthText = now.ToString("MMMM");
        YearText = now.ToString("yyyy");
        HoursText = now.ToString("HH:mm:ss");
    }
}

// App-wide singleton - both PosViewModel and AdminViewModel expose this
// same instance as a HeaderClock property so header XAML can bind to it
// with an ordinary compiled binding instead of an x:Static Source binding.
public static class HeaderClockService
{
    public static HeaderClockViewModel Current { get; } = new();
}