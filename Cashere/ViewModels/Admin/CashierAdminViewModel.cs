using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin;

public partial class CashierAdminViewModel : ViewModelBase
{
    private readonly ICashierAdminService _cashierAdmin;
    private int? _editingCashierId;

    public ObservableCollection<Cashier> Cashiers { get; } = new();
    public IReadOnlyList<UserRole> Roles { get; } = Enum.GetValues<UserRole>();

    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string _editorTitle = "NEW CASHIER";
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _formUsername = string.Empty;
    [ObservableProperty] private string _formDisplayName = string.Empty;
    [ObservableProperty] private UserRole _formRole = UserRole.Cashier;
    [ObservableProperty] private string _formPassword = string.Empty;

    public string PasswordFieldLabel => _editingCashierId is null
        ? "PASSWORD"
        : "PASSWORD (LEAVE BLANK TO KEEP CURRENT)";

    public CashierAdminViewModel(ICashierAdminService cashierAdmin)
    {
        _cashierAdmin = cashierAdmin;
    }

    public async Task LoadAsync()
    {
        var cashiers = await _cashierAdmin.GetAllCashiersAsync();
        Cashiers.Clear();
        foreach (var cashier in cashiers) Cashiers.Add(cashier);
    }

    [RelayCommand]
    private void AddNew()
    {
        _editingCashierId = null;
        EditorTitle = "NEW CASHIER";
        FormUsername = string.Empty;
        FormDisplayName = string.Empty;
        FormRole = UserRole.Cashier;
        FormPassword = string.Empty;
        ErrorMessage = null;
        OnPropertyChanged(nameof(PasswordFieldLabel));
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditCashier(Cashier? cashier)
    {
        if (cashier is null) return;

        _editingCashierId = cashier.Id;
        EditorTitle = "EDIT CASHIER";
        FormUsername = cashier.Username;
        FormDisplayName = cashier.DisplayName;
        FormRole = cashier.Role;
        FormPassword = string.Empty; // left blank = keep existing password
        ErrorMessage = null;
        OnPropertyChanged(nameof(PasswordFieldLabel));
        IsEditorOpen = true;
    }

    [RelayCommand]
    private async Task ToggleActive(Cashier? cashier)
    {
        if (cashier is null) return;
        await _cashierAdmin.SetActiveAsync(cashier.Id, !cashier.IsActive);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormDisplayName))
        {
            ErrorMessage = "Username and display name are required.";
            return;
        }

        if (_editingCashierId is null && string.IsNullOrWhiteSpace(FormPassword))
        {
            ErrorMessage = "A password is required for a new cashier.";
            return;
        }

        var input = new CashierInput(
            FormUsername, FormDisplayName, FormRole,
            string.IsNullOrWhiteSpace(FormPassword) ? null : FormPassword);

        try
        {
            if (_editingCashierId is int id)
            {
                await _cashierAdmin.UpdateCashierAsync(id, input);
            }
            else
            {
                await _cashierAdmin.CreateCashierAsync(input);
            }

            IsEditorOpen = false;
            await LoadAsync();
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelEdit() => IsEditorOpen = false;
}