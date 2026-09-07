using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cashere.ViewModels.Admin;

public partial class SalesReportViewModel : ViewModelBase
{
    private readonly ISalesReportService _reportService;

    public ObservableCollection<DailySalesRow> DailyBreakdown { get; } = new();

    // Defaults to the trailing 7 days - long enough for the daily breakdown
    // table to actually show a trend on first open, short enough to load fast.
    [ObservableProperty] private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date.AddDays(-6);
    [ObservableProperty] private DateTimeOffset? _toDate = DateTimeOffset.Now.Date;

    [ObservableProperty] private int _transactionCount;
    [ObservableProperty] private decimal _grossRevenue;
    [ObservableProperty] private decimal _totalDiscount;
    [ObservableProperty] private decimal _totalTax;
    [ObservableProperty] private decimal _totalCost;
    [ObservableProperty] private decimal _grossProfit;
    [ObservableProperty] private string? _errorMessage;

    public SalesReportViewModel(ISalesReportService reportService)
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

            var summary = await _reportService.GetSalesReportAsync(from, to);

            TransactionCount = summary.TransactionCount;
            GrossRevenue = summary.GrossRevenue;
            TotalDiscount = summary.TotalDiscount;
            TotalTax = summary.TotalTax;
            TotalCost = summary.TotalCost;
            GrossProfit = summary.GrossProfit;

            DailyBreakdown.Clear();
            foreach (var row in summary.DailyBreakdown) DailyBreakdown.Add(row);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load report: {ex.Message}";
        }
    }
}