using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

public partial class DataBackupSettingsViewModel : ViewModelBase
{
    private readonly IDataBackupService _backupService;

    public ObservableCollection<BackupFileInfo> Backups { get; } = new();

    [ObservableProperty] private string _databasePath = string.Empty;
    [ObservableProperty] private string _databaseSizeDisplay = string.Empty;
    [ObservableProperty] private string _lastModifiedDisplay = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public bool HasNoBackups => Backups.Count == 0;

    public DataBackupSettingsViewModel(IDataBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task LoadAsync()
    {
        var status = await _backupService.GetStatusAsync();
        DatabasePath = status.DatabasePath;
        DatabaseSizeDisplay = FormatBytes(status.FileSizeBytes);
        LastModifiedDisplay = status.LastModifiedUtc?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "Unknown";

        var backups = await _backupService.GetBackupsAsync();
        Backups.Clear();
        foreach (var backup in backups) Backups.Add(backup);
        OnPropertyChanged(nameof(HasNoBackups));
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