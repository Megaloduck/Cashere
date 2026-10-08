using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// First real Security screen - previously a placeholder blocked entirely on
// the login screen existing. Auto-lock is the one item here with an actual
// mechanism behind it (AutoLockService, armed from RootViewModel and reset
// from MainWindow on every input event); PIN sign-in is already satisfied
// by Login itself. The recent-activity list below is filled by the database
// audit trail; sensitive-action approvals and local DB encryption remain.
public partial class SecuritySettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;
    private readonly IAuditLogService? _auditLog;
    public ObservableCollection<AuditLogEntry> RecentActivity { get; } = new();
    public ObservableCollection<ActivityDayOption> ActivityDays { get; } = new();
    public IReadOnlyList<int> AuditRetentionOptions { get; } = new[] { 1, 3, 6, 12 };
    private List<AuditLogEntry> _cachedActivity = new();
    [ObservableProperty] private bool _hasNoRecentActivity = true;
    [ObservableProperty] private ActivityDayOption? _selectedActivityDay;

    public IReadOnlyList<int> TimeoutOptions { get; } = new[] { 1, 5, 10, 15, 30, 60 };

    [ObservableProperty] private bool _autoLockEnabled;
    [ObservableProperty] private int _autoLockTimeoutMinutes = 15;
    [ObservableProperty] private bool _autoDeleteAuditEnabled = true;
    [ObservableProperty] private int _auditRetentionMonths = 1;
    [ObservableProperty] private string? _statusMessage;

    partial void OnSelectedActivityDayChanged(ActivityDayOption? value) => ApplyActivityDay(value);

    public SecuritySettingsViewModel(IShopContextService shopContext, IAuditLogService? auditLog = null)
    {
        _shopContext = shopContext;
        _auditLog = auditLog;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSecuritySettingsAsync();
        AutoLockEnabled = settings.AutoLockEnabled;
        AutoLockTimeoutMinutes = settings.AutoLockTimeoutMinutes;
        AutoDeleteAuditEnabled = settings.AutoDeleteAuditEnabled;
        AuditRetentionMonths = settings.AuditRetentionMonths;
        await RefreshAuditAsync();
    }

    [RelayCommand]
    private async Task RefreshAudit() => await RefreshAuditAsync();

    private async Task RefreshAuditAsync()
    {
        var selectedDate = SelectedActivityDay?.Date;
        _cachedActivity = _auditLog is null ? new List<AuditLogEntry>() : await _auditLog.GetRecentAsync(1000);
        ActivityDays.Clear();
        foreach (var date in _cachedActivity.Select(entry => entry.OccurredAtUtc.ToLocalTime().Date).Distinct().OrderByDescending(date => date))
            ActivityDays.Add(new ActivityDayOption(date));
        SelectedActivityDay = ActivityDays.FirstOrDefault(day => day.Date == selectedDate) ?? ActivityDays.FirstOrDefault();
        if (SelectedActivityDay is null) ApplyActivityDay(null);
    }

    private void ApplyActivityDay(ActivityDayOption? day)
    {
        RecentActivity.Clear();
        if (day is not null)
            foreach (var entry in _cachedActivity.Where(entry => entry.OccurredAtUtc.ToLocalTime().Date == day.Date))
                RecentActivity.Add(entry);
        HasNoRecentActivity = RecentActivity.Count == 0;
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();
        settings.AutoLockEnabled = AutoLockEnabled;
        settings.AutoLockTimeoutMinutes = AutoLockTimeoutMinutes;
        settings.AutoDeleteAuditEnabled = AutoDeleteAuditEnabled;
        settings.AuditRetentionMonths = Math.Clamp(AuditRetentionMonths, 1, 12);

        await _shopContext.UpdateSettingsAsync(settings);

        // Applied immediately, same "live" feel Preferences' ThemeApplier
        // call already has - no restart needed for the new timeout to take
        // effect.
        AutoLockService.Configure(AutoLockEnabled, AutoLockTimeoutMinutes);

        StatusMessage = "Saved.";
        await RefreshAuditAsync();
    }

    [RelayCommand]
    private async Task Undo(AuditLogEntry entry)
    {
        var succeeded = _auditLog is not null && await _auditLog.UndoAsync(entry.Id);
        StatusMessage = succeeded
            ? "The change was undone."
            : "The record has changed since this audit entry, so it was not undone.";
        if (succeeded) await ReloadSecuritySettingsAsync();
        await RefreshAuditAsync();
    }

    [RelayCommand]
    private async Task Redo(AuditLogEntry entry)
    {
        var succeeded = _auditLog is not null && await _auditLog.RedoAsync(entry.Id);
        StatusMessage = succeeded
            ? "The change was redone."
            : "The record has changed since it was undone, so it was not redone.";
        if (succeeded) await ReloadSecuritySettingsAsync();
        await RefreshAuditAsync();
    }

    private async Task ReloadSecuritySettingsAsync()
    {
        var settings = await _shopContext.GetSecuritySettingsAsync();
        AutoLockEnabled = settings.AutoLockEnabled;
        AutoLockTimeoutMinutes = settings.AutoLockTimeoutMinutes;
        AutoDeleteAuditEnabled = settings.AutoDeleteAuditEnabled;
        AuditRetentionMonths = settings.AuditRetentionMonths;
        AutoLockService.Configure(AutoLockEnabled, AutoLockTimeoutMinutes);
    }
}

public sealed record ActivityDayOption(DateTime Date)
{
    public string DisplayName => Date.ToString("dddd, dd MMM yyyy");
}
