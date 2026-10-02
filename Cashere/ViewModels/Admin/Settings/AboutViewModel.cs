using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using Avalonia.Platform;
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

    public ObservableCollection<ThirdPartyLicenseInfo> ThirdPartyLicenses { get; } = new();

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

        LoadThirdPartyLicenses();
    }

    private void LoadThirdPartyLicenses()
    {
        ThirdPartyLicenses.Clear();
        var assetUri = new Uri("avares://Cashere/Assets/ThirdPartyLicenses.tsv");
        using var stream = AssetLoader.Open(assetUri);
        using var reader = new StreamReader(stream);
        _ = reader.ReadLine(); // TSV header

        while (reader.ReadLine() is { } line)
        {
            var columns = line.Split('\t');
            if (columns.Length < 5) continue;
            ThirdPartyLicenses.Add(new ThirdPartyLicenseInfo(
                columns[0], columns[1], columns[2], columns[3], columns[4]));
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public sealed record ThirdPartyLicenseInfo(
    string Package,
    string Version,
    string License,
    string LicenseUrl,
    string PackageUrl);
