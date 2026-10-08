using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Cashere.ViewModels.Admin.Settings;

public partial class DataBackupSettingsViewModel : ViewModelBase
{
    private readonly IDataBackupService _backupService;
    private readonly IShopContextService _shopContext;
    private readonly IReportExportService? _reportExport;
    private readonly ICashierAdminService _cashierAdmin;
    private readonly IHistoricalSalesImportService? _historicalSalesImport;
    private readonly int _currentCashierId;
    private readonly UserRole _currentRole;
    private readonly string _currentUsername;

    public ObservableCollection<BackupFileInfo> Backups { get; } = new();

    [ObservableProperty] private string _databasePath = string.Empty;
    [ObservableProperty] private string _databaseSizeDisplay = string.Empty;
    [ObservableProperty] private string _lastModifiedDisplay = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _scheduledBackupsEnabled;
    [ObservableProperty] private string _scheduledBackupTime = "23:00";
    [ObservableProperty] private string _automaticBackupRetentionCount = "30";

    [ObservableProperty] private DateTimeOffset? _exportFromDate = DateTimeOffset.Now.Date.AddDays(-29);
    [ObservableProperty] private DateTimeOffset? _exportToDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string? _exportStatusMessage;
    [ObservableProperty] private bool _isImportingSales;
    [ObservableProperty] private string? _importStatusMessage;

    [ObservableProperty] private bool _isResetPromptOpen;
    [ObservableProperty] private string _resetPin = string.Empty;
    [ObservableProperty] private string? _resetErrorMessage;
    [ObservableProperty] private bool _isResetting;

    public bool CanResetDatabase => _currentRole == UserRole.Owner;
    public bool IsReportExportAvailable => _reportExport is not null;
    public bool CanImportHistoricalSales => _currentRole == UserRole.Owner && _historicalSalesImport is not null;
    public bool HasNoBackups => Backups.Count == 0;

    // Bubbled up the same way Staff.ManageCashiersRequested is, so
    // AdminViewModel can forward it into the existing LogoutRequested chain -
    // a reset deletes the signed-in cashier's own row, so the session can't
    // meaningfully continue.
    public event Action? DatabaseWasReset;

    public DataBackupSettingsViewModel(
        IDataBackupService backupService,
        IShopContextService shopContext,
        IReportExportService? reportExport,
        ICashierAdminService cashierAdmin,
        int currentCashierId,
        UserRole currentRole,
        string currentUsername,
        IHistoricalSalesImportService? historicalSalesImport)
    {
        _backupService = backupService;
        _shopContext = shopContext;
        _reportExport = reportExport;
        _cashierAdmin = cashierAdmin;
        _currentCashierId = currentCashierId;
        _currentRole = currentRole;
        _currentUsername = currentUsername;
        _historicalSalesImport = historicalSalesImport;

        // Keep HasNoBackups in sync whenever the collection changes.
        Backups.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoBackups));
    }

    public async Task<byte[]?> CreateSalesImportTemplateAsync()
    {
        if (!CanImportHistoricalSales || _historicalSalesImport is null) return null;
        ImportStatusMessage = null;
        try
        {
            return await _historicalSalesImport.CreateTemplateAsync();
        }
        catch (Exception ex)
        {
            ImportStatusMessage = $"Template failed: {ex.Message}";
            return null;
        }
    }

    public async Task ImportHistoricalSalesAsync(byte[] workbookBytes)
    {
        if (!CanImportHistoricalSales || _historicalSalesImport is null) return;
        ImportStatusMessage = null;
        IsImportingSales = true;
        try
        {
            var result = await _historicalSalesImport.ImportAsync(workbookBytes, _currentCashierId, _currentRole);
            ImportStatusMessage = $"Imported {result.ImportedSales} sales; skipped {result.SkippedDuplicates} duplicate sale numbers. Current stock was not changed.";
        }
        catch (Exception ex) { ImportStatusMessage = $"Import failed: {ex.Message}"; }
        finally { IsImportingSales = false; }
    }

    public async Task LoadAsync()
    {
        var status = await _backupService.GetStatusAsync();
        DatabasePath = status.DatabasePath;
        DatabaseSizeDisplay = FormatBytes(status.FileSizeBytes);

        var settings = await _shopContext.GetSettingsAsync();
        if (settings is not null)
        {
            ScheduledBackupsEnabled = settings.ScheduledBackupsEnabled;
            ScheduledBackupTime = settings.ScheduledBackupTime;
            AutomaticBackupRetentionCount = settings.AutomaticBackupRetentionCount.ToString();
        }

        // FileInfo.LastWriteTimeUtc already carries Kind=Utc correctly (unlike
        // values round-tripped through SQLite), but this still goes through
        // the shared ClockPreferenceService rather than its own .ToLocalTime()
        // call, so every displayed timestamp in the app is guaranteed to stay
        // derived from the exact same conversion - no separate code path that
        // could quietly drift from the rest of the UI.
        LastModifiedDisplay = status.LastModifiedUtc is { } lastModified
            ? ClockPreferenceService.ToDisplay(lastModified).ToString("dd MMM yyyy HH:mm")
            : "Unknown";

        var backups = await _backupService.GetBackupsAsync();
        Backups.Clear();
        foreach (var backup in backups) Backups.Add(backup);
    }

    [RelayCommand]
    private async Task CreateBackup()
    {
        StatusMessage = null;
        IsBusy = true;
        try
        {
            var backup = await _backupService.CreateBackupAsync();
            await LoadAsync();
            StatusMessage = $"Backup created: {backup.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveBackupSchedule()
    {
        StatusMessage = null;
        if (!TimeSpan.TryParseExact(ScheduledBackupTime, "hh\\:mm", System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            StatusMessage = "Enter the daily backup time as HH:mm (24-hour time).";
            return;
        }
        if (!int.TryParse(AutomaticBackupRetentionCount, out var retention) || retention is < 1 or > 365)
        {
            StatusMessage = "Keep between 1 and 365 automatic backups.";
            return;
        }

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();
        settings.ScheduledBackupsEnabled = ScheduledBackupsEnabled;
        settings.ScheduledBackupTime = ScheduledBackupTime;
        settings.AutomaticBackupRetentionCount = retention;
        await _shopContext.UpdateSettingsAsync(settings);
        StatusMessage = "Backup schedule saved.";
    }

    [RelayCommand]
    private async Task ExportSalesReport()
    {
        if (_reportExport is null) return;

        ExportStatusMessage = null;
        IsExporting = true;
        try
        {
            var from = (ExportFromDate ?? DateTimeOffset.Now).Date;
            var to = (ExportToDate ?? DateTimeOffset.Now).Date;
            var path = await _reportExport.ExportSalesReportAsync(from, to);
            ExportStatusMessage = $"Exported to {path}";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task Restore(BackupFileInfo? backup)
    {
        if (backup is null) return;

        StatusMessage = null;
        IsBusy = true;
        try
        {
            await _backupService.RestoreFromBackupAsync(backup.FullPath);
            StatusMessage = "Restored. Restart Cashere for the restored data to take effect.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBackup(BackupFileInfo? backup)
    {
        if (backup is null) return;

        await _backupService.DeleteBackupAsync(backup.FullPath);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void OpenResetPrompt()
    {
        if (!CanResetDatabase) return;
        ResetPin = string.Empty;
        ResetErrorMessage = null;
        IsResetPromptOpen = true;
    }

    [RelayCommand]
    private void CancelReset()
    {
        IsResetPromptOpen = false;
        ResetPin = string.Empty;
        ResetErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmReset()
    {
        if (!CanResetDatabase || IsResetting) return;

        ResetErrorMessage = null;

        if (string.IsNullOrWhiteSpace(ResetPin))
        {
            ResetErrorMessage = "Enter your PIN to confirm.";
            return;
        }

        IsResetting = true;
        try
        {
            var verified = await _cashierAdmin.VerifyCredentialsAsync(_currentUsername, ResetPin);
            if (verified is null || verified.Role != UserRole.Owner)
            {
                ResetErrorMessage = "Incorrect PIN.";
                return;
            }

            await _backupService.ResetAllDataAsync();

            IsResetPromptOpen = false;
            DatabaseWasReset?.Invoke();
        }
        catch (Exception ex)
        {
            ResetErrorMessage = $"Reset failed: {ex.Message}";
        }
        finally
        {
            ResetPin = string.Empty;
            IsResetting = false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {units[unitIndex]}";
    }
}
