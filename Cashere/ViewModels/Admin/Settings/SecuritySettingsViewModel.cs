using System.Collections.Generic;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// First real Security screen - previously a placeholder blocked entirely on
// the login screen existing. Auto-lock is the one item here with an actual
// mechanism behind it (AutoLockService, armed from RootViewModel and reset
// from MainWindow on every input event); PIN sign-in is already satisfied
// by Login itself, and everything else from the original placeholder list
// (manager authorization, audit log, local DB encryption) stays "coming
// soon" until there's a real feature to attach it to.
public partial class SecuritySettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    public IReadOnlyList<int> TimeoutOptions { get; } = new[] { 1, 5, 10, 15, 30, 60 };

    [ObservableProperty] private bool _autoLockEnabled;
    [ObservableProperty] private int _autoLockTimeoutMinutes = 15;
    [ObservableProperty] private string? _statusMessage;

    public SecuritySettingsViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSecuritySettingsAsync();
        AutoLockEnabled = settings.AutoLockEnabled;
        AutoLockTimeoutMinutes = settings.AutoLockTimeoutMinutes;
    }

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();
        settings.AutoLockEnabled = AutoLockEnabled;
        settings.AutoLockTimeoutMinutes = AutoLockTimeoutMinutes;

        await _shopContext.UpdateSettingsAsync(settings);

        // Applied immediately, same "live" feel Preferences' ThemeApplier
        // call already has - no restart needed for the new timeout to take
        // effect.
        AutoLockService.Configure(AutoLockEnabled, AutoLockTimeoutMinutes);

        StatusMessage = "Saved.";
    }
}