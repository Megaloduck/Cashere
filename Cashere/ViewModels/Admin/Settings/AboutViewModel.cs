using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

public partial class AboutViewModel : ViewModelBase
{
    private readonly IAboutInfoService _aboutInfo;

    [ObservableProperty] private string _appVersion = string.Empty;
    [ObservableProperty] private string _databasePath = string.Empty;
    [ObservableProperty] private string _databaseSizeDisplay = string.Empty;
    [ObservableProperty] private string _lastModifiedDisplay = string.Empty;
    [ObservableProperty] private int _appliedMigrationCount;

    public AboutViewModel(IAboutInfoService aboutInfo)
    {
        _aboutInfo = aboutInfo;
    }

    public async Task LoadAsync()
    {
        var info = await _aboutInfo.GetAboutInfoAsync();

        AppVersion = info.AppVersion;
        DatabasePath = info.DatabasePath;
        DatabaseSizeDisplay = info.DatabaseSizeDisplay;
        LastModifiedDisplay = info.LastModifiedDisplay;
        AppliedMigrationCount = info.AppliedMigrationCount;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}