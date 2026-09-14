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

public partial class CashRegisterViewModel : ViewModelBase
{
    private readonly IShiftAdminService _shiftAdmin;
    private readonly int _currentCashierId;
    private int? _openShiftId;

    public ObservableCollection<ShiftListItem> RecentShifts { get; } = new();

    [ObservableProperty] private bool _hasOpenShift;
    [ObservableProperty] private DateTime? _openedAt;
    [ObservableProperty] private decimal _startingCash;
    [ObservableProperty] private decimal _expectedCash;
    [ObservableProperty] private string? _statusMessage;

    [ObservableProperty] private string _formStartingCash = "0";
    [ObservableProperty] private string _formNotes = string.Empty;

    [ObservableProperty] private string _formActualCash = "0";
    [ObservableProperty] private string _formCloseNotes = string.Empty;

    public CashRegisterViewModel(IShiftAdminService shiftAdmin, int currentCashierId)
    {
        _shiftAdmin = shiftAdmin;
        _currentCashierId = currentCashierId;
    }

    public async Task LoadAsync()
    {
        StatusMessage = null;

        var open = await _shiftAdmin.GetOpenShiftAsync();
        HasOpenShift = open is not null;

        if (open is not null)
        {
            _openShiftId = open.Id;
            OpenedAt = open.OpenedAt;
            StartingCash = open.StartingCash;
            ExpectedCash = await _shiftAdmin.GetExpectedCashAsync(open.Id);
            FormActualCash = ExpectedCash.ToString();
        }
        else
        {
            _openShiftId = null;
            OpenedAt = null;
        }

        var recent = await _shiftAdmin.GetRecentShiftsAsync();
        RecentShifts.Clear();
        foreach (var shift in recent) RecentShifts.Add(shift);
    }

    [RelayCommand]
    private async Task OpenShift()
    {
        StatusMessage = null;

        if (!decimal.TryParse(FormStartingCash, out var startingCash) || startingCash < 0)
        {
            StatusMessage = "Starting cash must be a valid, non-negative number.";
            return;
        }

        try
        {
            await _shiftAdmin.OpenShiftAsync(_currentCashierId, startingCash, FormNotes);
            FormStartingCash = "0";
            FormNotes = string.Empty;
            await LoadAsync();
            StatusMessage = "Shift opened.";
        }
        catch (AdminValidationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CloseShift()
    {
        StatusMessage = null;

        if (_openShiftId is not int shiftId)
        {
            StatusMessage = "No shift is currently open.";
            return;
        }

        if (!decimal.TryParse(FormActualCash, out var actualCash) || actualCash < 0)
        {
            StatusMessage = "Actual cash must be a valid, non-negative number.";
            return;
        }

        try
        {
            await _shiftAdmin.CloseShiftAsync(shiftId, actualCash, FormCloseNotes);
            FormActualCash = "0";
            FormCloseNotes = string.Empty;
            await LoadAsync();
            StatusMessage = "Shift closed.";
        }
        catch (AdminValidationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}