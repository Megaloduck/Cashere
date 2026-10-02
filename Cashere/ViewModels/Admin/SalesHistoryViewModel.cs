using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Cashere.Models;

namespace Cashere.ViewModels.Admin;

public partial class SalesHistoryViewModel : ViewModelBase
{
    private readonly ISalesReportService _reportService;
    private readonly IRefundService? _refundService;
    private readonly int _currentCashierId;
    private readonly UserRole _currentRole;
    private readonly ICashierAdminService? _cashierAdmin;
    private readonly bool _cashierCanViewOwnSalesHistory;
    private readonly bool _cashierCanRequestRefunds;
    private readonly bool _cashierCanRequestVoids;
    private int? _activeSaleId;

    public ObservableCollection<SaleListItem> Sales { get; } = new();

    [ObservableProperty] private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private DateTimeOffset? _toDate = DateTimeOffset.Now.Date;

    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private SaleDetail? _selectedSaleDetail;
    [ObservableProperty] private string? _errorMessage;

    public bool RequiresManagerApproval => _currentRole == UserRole.Cashier;
    public bool CanManageRefunds => _refundService is not null &&
        (_currentRole != UserRole.Cashier || _cashierCanViewOwnSalesHistory && _cashierCanRequestRefunds);
    public bool CanManageVoids => _refundService is not null &&
        (_currentRole != UserRole.Cashier || _cashierCanViewOwnSalesHistory && _cashierCanRequestVoids);
    public bool CanManage => CanManageRefunds || CanManageVoids;
    public bool CanViewProfitMetrics => _currentRole != UserRole.Cashier;

    [ObservableProperty] private bool _isRefundDialogOpen;
    [ObservableProperty] private string _refundReason = string.Empty;
    [ObservableProperty] private string? _refundErrorMessage;
    [ObservableProperty] private bool _isProcessingRefund;
    [ObservableProperty] private string _authorizationUsername = string.Empty;
    [ObservableProperty] private string _authorizationPin = string.Empty;
    public ObservableCollection<RefundLineFormItem> RefundLines { get; } = new();

    // Lightweight confirm-with-reason prompt for the one-click VOID action -
    // deliberately a separate overlay from the line picker, since voiding
    // doesn't need a quantity per line.
    [ObservableProperty] private bool _isVoidPromptOpen;
    [ObservableProperty] private string _voidReason = string.Empty;
    [ObservableProperty] private string? _voidErrorMessage;
    [ObservableProperty] private bool _isProcessingVoid;

    public SalesHistoryViewModel(
        ISalesReportService reportService,
        UserRole currentRole,
        int currentCashierId,
        IRefundService? refundService = null,
        ICashierAdminService? cashierAdmin = null,
        bool cashierCanViewOwnSalesHistory = false,
        bool cashierCanRequestRefunds = false,
        bool cashierCanRequestVoids = false)
    {
        _reportService = reportService;
        _currentRole = currentRole;
        _currentCashierId = currentCashierId;
        _refundService = refundService;
        _cashierAdmin = cashierAdmin;
        _cashierCanViewOwnSalesHistory = cashierCanViewOwnSalesHistory;
        _cashierCanRequestRefunds = cashierCanRequestRefunds;
        _cashierCanRequestVoids = cashierCanRequestVoids;
    }

    public async Task LoadAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task Refresh() => await RefreshAsync();

    [RelayCommand]
    private void SetToday()
    {
        var today = DateTimeOffset.Now.Date;
        FromDate = today;
        ToDate = today;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTimeOffset.Now.Date;
        FromDate = today.AddDays(-(int)today.DayOfWeek);
        ToDate = today;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void SetThisMonth()
    {
        var today = DateTimeOffset.Now.Date;
        FromDate = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
        ToDate = today;
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        if (_currentRole == UserRole.Cashier && !_cashierCanViewOwnSalesHistory)
        {
            Sales.Clear();
            return;
        }
        try
        {
            var from = (FromDate ?? DateTimeOffset.Now).Date;
            var to = (ToDate ?? DateTimeOffset.Now).Date;

            var results = await _reportService.GetSalesHistoryAsync(from, to,
                _currentRole == UserRole.Cashier ? _currentCashierId : null);
            Sales.Clear();
            foreach (var sale in results) Sales.Add(sale);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load sales: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ViewSale(SaleListItem? item)
    {
        if (item is null) return;

        _activeSaleId = item.Id;
        SelectedSaleDetail = await _reportService.GetSaleDetailAsync(item.Id,
            _currentRole == UserRole.Cashier ? _currentCashierId : null);
        IsDetailOpen = SelectedSaleDetail is not null;
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
        _activeSaleId = null;
    }

    [RelayCommand]
    private async Task OpenRefundDialog(SaleListItem? item)
    {
        if (!CanManageRefunds || _refundService is null) return;

        var target = item ?? Sales.FirstOrDefault(s => s.Id == _activeSaleId);
        if (target is null || !Sales.Any(s => s.Id == target.Id)) return;

        RefundErrorMessage = null;
        AuthorizationUsername = string.Empty;
        AuthorizationPin = string.Empty;
        RefundReason = string.Empty;
        _activeSaleId = target.Id;

        var refundable = await _refundService.GetRefundableSaleAsync(target.Id);
        if (refundable is null)
        {
            RefundErrorMessage = "Could not load this sale for refund.";
            return;
        }

        RefundLines.Clear();
        foreach (var line in refundable.Lines)
        {
            RefundLines.Add(new RefundLineFormItem(
                line.SaleItemId, line.ProductName, line.OriginalQuantity,
                line.AlreadyRefundedQuantity, line.RefundableQuantity, line.UnitPrice));
        }

        IsRefundDialogOpen = true;
    }

    [RelayCommand]
    private void CancelRefund()
    {
        IsRefundDialogOpen = false;
        RefundLines.Clear();
        RefundErrorMessage = null;
        AuthorizationPin = string.Empty;
    }

    [RelayCommand]
    private async Task ProcessRefund()
    {
        if (!CanManageRefunds || _refundService is null || _activeSaleId is not int saleId) return;

        RefundErrorMessage = null;

        var lines = new List<RefundLineInput>();
        foreach (var line in RefundLines)
        {
            if (!int.TryParse(line.RefundQuantity, out var quantity) || quantity < 0)
            {
                RefundErrorMessage = $"'{line.ProductName}': enter a valid whole number.";
                return;
            }

            if (quantity > line.RefundableQuantity)
            {
                RefundErrorMessage = $"'{line.ProductName}': only {line.RefundableQuantity} can still be refunded.";
                return;
            }

            if (quantity > 0)
            {
                lines.Add(new RefundLineInput(line.SaleItemId, quantity));
            }
        }

        if (lines.Count == 0)
        {
            RefundErrorMessage = "Enter a quantity greater than zero for at least one line.";
            return;
        }

        var approvingManager = await VerifyManagerApprovalAsync(message => RefundErrorMessage = message);
        if (RequiresManagerApproval && approvingManager is null) return;

        IsProcessingRefund = true;
        try
        {
            var previousActor = CurrentCashierContext.Snapshot();
            if (approvingManager is not null)
                CurrentCashierContext.Set(approvingManager.Id, approvingManager.DisplayName);
            try
            {
            await _refundService.RefundLinesAsync(
                saleId, approvingManager?.Id ?? _currentCashierId, lines,
                string.IsNullOrWhiteSpace(RefundReason) ? null : RefundReason.Trim());
            }
            finally
            {
                RestoreActor(previousActor);
            }

            IsRefundDialogOpen = false;
            RefundLines.Clear();
            await RefreshAsync();
            SelectedSaleDetail = await _reportService.GetSaleDetailAsync(saleId,
                _currentRole == UserRole.Cashier ? _currentCashierId : null);
        }
        catch (AdminValidationException ex)
        {
            RefundErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessingRefund = false;
            AuthorizationPin = string.Empty;
        }
    }

    [RelayCommand]
    private void OpenVoidPrompt(SaleListItem? item)
    {
        if (!CanManageVoids || _refundService is null) return;

        var target = item ?? Sales.FirstOrDefault(s => s.Id == _activeSaleId);
        if (target is null || !Sales.Any(s => s.Id == target.Id)) return;

        _activeSaleId = target.Id;
        VoidReason = string.Empty;
        VoidErrorMessage = null;
        AuthorizationUsername = string.Empty;
        AuthorizationPin = string.Empty;
        IsVoidPromptOpen = true;
    }

    [RelayCommand]
    private void CancelVoid()
    {
        IsVoidPromptOpen = false;
        VoidErrorMessage = null;
        AuthorizationPin = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmVoid()
    {
        if (!CanManageVoids || _refundService is null || _activeSaleId is not int saleId) return;

        var approvingManager = await VerifyManagerApprovalAsync(message => VoidErrorMessage = message);
        if (RequiresManagerApproval && approvingManager is null) return;

        IsProcessingVoid = true;
        VoidErrorMessage = null;
        try
        {
            var previousActor = CurrentCashierContext.Snapshot();
            if (approvingManager is not null)
                CurrentCashierContext.Set(approvingManager.Id, approvingManager.DisplayName);
            try
            {
            await _refundService.VoidSaleAsync(
                saleId, approvingManager?.Id ?? _currentCashierId,
                string.IsNullOrWhiteSpace(VoidReason) ? null : VoidReason.Trim());
            }
            finally
            {
                RestoreActor(previousActor);
            }

            IsVoidPromptOpen = false;
            await RefreshAsync();
            SelectedSaleDetail = await _reportService.GetSaleDetailAsync(saleId,
                _currentRole == UserRole.Cashier ? _currentCashierId : null);
        }
        catch (AdminValidationException ex)
        {
            VoidErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessingVoid = false;
            AuthorizationPin = string.Empty;
        }
    }

    private async Task<Cashier?> VerifyManagerApprovalAsync(Action<string> reportError)
    {
        if (!RequiresManagerApproval) return null;
        if (_cashierAdmin is null)
        {
            reportError("Manager approval is unavailable in this build.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(AuthorizationUsername) || string.IsNullOrWhiteSpace(AuthorizationPin))
        {
            reportError("Enter a Manager or Owner username and PIN to approve this action.");
            return null;
        }

        Cashier? approver;
        try
        {
            approver = await _cashierAdmin.VerifyCredentialsAsync(AuthorizationUsername.Trim(), AuthorizationPin);
        }
        catch (Exception ex)
        {
            AuthorizationPin = string.Empty;
            reportError($"Could not verify manager approval: {ex.Message}");
            return null;
        }
        AuthorizationPin = string.Empty;
        if (approver is null || approver.Role == UserRole.Cashier)
        {
            reportError("The credentials are invalid or the account is not an Owner or Manager.");
            return null;
        }
        return approver;
    }

    private static void RestoreActor((int? CashierId, string DisplayName) previousActor)
    {
        if (previousActor.CashierId is int cashierId)
            CurrentCashierContext.Set(cashierId, previousActor.DisplayName);
        else
            CurrentCashierContext.Clear();
    }
}
