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
    private int? _activeSaleId;

    public ObservableCollection<SaleListItem> Sales { get; } = new();

    [ObservableProperty] private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private DateTimeOffset? _toDate = DateTimeOffset.Now.Date;

    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private SaleDetail? _selectedSaleDetail;
    [ObservableProperty] private string? _errorMessage;

    // Void/Refund is money-sensitive, same view-vs-manage split as every
    // other admin screen - Cashier logins can browse history and open the
    // detail panel but never see these buttons.
    public bool CanManage => RolePermissions.CanManage(_currentRole) && _refundService is not null;

    [ObservableProperty] private bool _isRefundDialogOpen;
    [ObservableProperty] private string _refundReason = string.Empty;
    [ObservableProperty] private string? _refundErrorMessage;
    [ObservableProperty] private bool _isProcessingRefund;
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
        IRefundService? refundService = null)
    {
        _reportService = reportService;
        _currentRole = currentRole;
        _currentCashierId = currentCashierId;
        _refundService = refundService;
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
        try
        {
            var from = (FromDate ?? DateTimeOffset.Now).Date;
            var to = (ToDate ?? DateTimeOffset.Now).Date;

            var results = await _reportService.GetSalesHistoryAsync(from, to);
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
        SelectedSaleDetail = await _reportService.GetSaleDetailAsync(item.Id);
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
        if (!CanManage || _refundService is null) return;

        var target = item ?? Sales.FirstOrDefault(s => s.Id == _activeSaleId);
        if (target is null) return;

        RefundErrorMessage = null;
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
    }

    [RelayCommand]
    private async Task ProcessRefund()
    {
        if (!CanManage || _refundService is null || _activeSaleId is not int saleId) return;

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

        IsProcessingRefund = true;
        try
        {
            await _refundService.RefundLinesAsync(
                saleId, _currentCashierId, lines,
                string.IsNullOrWhiteSpace(RefundReason) ? null : RefundReason.Trim());

            IsRefundDialogOpen = false;
            RefundLines.Clear();
            await RefreshAsync();
            SelectedSaleDetail = await _reportService.GetSaleDetailAsync(saleId);
        }
        catch (AdminValidationException ex)
        {
            RefundErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessingRefund = false;
        }
    }

    [RelayCommand]
    private void OpenVoidPrompt(SaleListItem? item)
    {
        if (!CanManage || _refundService is null) return;

        var target = item ?? Sales.FirstOrDefault(s => s.Id == _activeSaleId);
        if (target is null) return;

        _activeSaleId = target.Id;
        VoidReason = string.Empty;
        VoidErrorMessage = null;
        IsVoidPromptOpen = true;
    }

    [RelayCommand]
    private void CancelVoid()
    {
        IsVoidPromptOpen = false;
        VoidErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmVoid()
    {
        if (!CanManage || _refundService is null || _activeSaleId is not int saleId) return;

        IsProcessingVoid = true;
        VoidErrorMessage = null;
        try
        {
            await _refundService.VoidSaleAsync(
                saleId, _currentCashierId,
                string.IsNullOrWhiteSpace(VoidReason) ? null : VoidReason.Trim());

            IsVoidPromptOpen = false;
            await RefreshAsync();
            SelectedSaleDetail = await _reportService.GetSaleDetailAsync(saleId);
        }
        catch (AdminValidationException ex)
        {
            VoidErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessingVoid = false;
        }
    }
}