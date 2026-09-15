using System;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels;

// Gate screen shown before ShellViewModel exists. A "PIN" here is nothing
// special server-side - it's just a short string run through the exact same
// ICashierAdminService.VerifyCredentialsAsync/Pbkdf2PasswordHasher path a
// full password would use, so no schema or hashing changes were needed.
public partial class LoginViewModel : ViewModelBase
{
    private const int MaxPinLength = 12;

    private readonly ICashierAdminService _cashierAdmin;

    // Raised once VerifyCredentialsAsync returns a real cashier - RootViewModel
    // subscribes to this to build and switch to the Pos/Admin shell.
    public event Action<Cashier>? LoginSucceeded;

    [ObservableProperty]
    private string _shopName;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _pin = string.Empty;

    // Drives TextBox.RevealPassword on the PIN field via the eye icon in
    // its InnerRightContent - purely a display concern, never changes what
    // actually gets sent to VerifyCredentialsAsync.
    [ObservableProperty]
    private bool _isPinVisible;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public bool CanSubmit => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Pin);

    public LoginViewModel(ICashierAdminService cashierAdmin, string shopName)
    {
        _cashierAdmin = cashierAdmin;
        _shopName = string.IsNullOrWhiteSpace(shopName) ? "Cashere" : shopName;
    }

    partial void OnUsernameChanged(string value)
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnPinChanged(string value)
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    [RelayCommand]
    private void AppendDigit(string? digit)
    {
        if (string.IsNullOrEmpty(digit) || Pin.Length >= MaxPinLength) return;
        Pin += digit;
    }

    [RelayCommand]
    private void Backspace()
    {
        if (Pin.Length == 0) return;
        Pin = Pin[..^1];
    }

    [RelayCommand]
    private void ClearPin() => Pin = string.Empty;

    [RelayCommand]
    private void TogglePinVisibility() => IsPinVisible = !IsPinVisible;

    [RelayCommand]
    private async Task Login()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var cashier = await _cashierAdmin.VerifyCredentialsAsync(Username.Trim(), Pin);
            if (cashier is null)
            {
                ErrorMessage = "Incorrect username or PIN.";
                Pin = string.Empty;
                return;
            }

            LoginSucceeded?.Invoke(cashier);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}