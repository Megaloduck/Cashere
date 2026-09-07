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

namespace Cashere.ViewModels.Admin;

public partial class SalesHistoryViewModel : ViewModelBase
{
    private readonly ISalesReportService _reportService;

    public ObservableCollection<SaleListItem> Sales { get; } = new();

    [ObservableProperty] private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private DateTimeOffset? _toDate = DateTimeOffset.Now.Date;

    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private SaleDetail? _selectedSaleDetail;
    [ObservableProperty] private string? _errorMessage;

    public SalesHistoryViewModel(ISalesReportService reportService)
    {
        _reportService = reportService;
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

        SelectedSaleDetail = await _reportService.GetSaleDetailAsync(item.Id);
        IsDetailOpen = SelectedSaleDetail is not null;
    }

    [RelayCommand]
    private void CloseDetail() => IsDetailOpen = false;
}