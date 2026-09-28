using System;
using Cashere.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Admin.Settings;

// One editable row in the Business Hours grid - mirrors PurchaseLineViewModel's
// role as a form-only wrapper around a plain data shape (BusinessDayHours)
// that only becomes real settings once Save() runs.
public partial class BusinessDayFormItem : ViewModelBase
{
    public DayOfWeek Day { get; }
    public string DayLabel => Day.ToString().ToUpperInvariant();

    [ObservableProperty] private bool _isClosed;
    [ObservableProperty] private string _openTime;
    [ObservableProperty] private string _closeTime;

    public BusinessDayFormItem(BusinessDayHours source)
    {
        Day = source.Day;
        _isClosed = source.IsClosed;
        _openTime = source.OpenTime?.ToString(@"hh\:mm") ?? "09:00";
        _closeTime = source.CloseTime?.ToString(@"hh\:mm") ?? "17:00";
    }

    public BusinessDayHours ToModel() => new(
        Day,
        IsClosed,
        IsClosed ? null : (TimeSpan.TryParse(OpenTime, out var open) ? open : null),
        IsClosed ? null : (TimeSpan.TryParse(CloseTime, out var close) ? close : null));
}
