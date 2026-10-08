using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin.Settings;

// Store-wide inventory defaults and stock-enforcement rules, backed by the
// same single-row ReceiptAdmin table as Business Info/Receipts/Network.
// TrackInventory and OutOfStockBehavior are read directly by SaleService on
// every checkout; DefaultLowStockThreshold is read by ProductAdminViewModel
// when prefilling a new product's form; AutoGenerateSku/AutoGenerateBarcode
// are read by ProductAdminService when a new product is saved with either
// field left blank.
public partial class InventorySettingsViewModel : ViewModelBase
{
    private readonly IShopContextService _shopContext;

    public IReadOnlyList<OutOfStockBehavior> OutOfStockBehaviors { get; } = Enum.GetValues<OutOfStockBehavior>();

    [ObservableProperty] private bool _trackInventory = true;
    [ObservableProperty] private OutOfStockBehavior _outOfStockBehavior = OutOfStockBehavior.Block;
    [ObservableProperty] private string _defaultLowStockThreshold = "5";
    [ObservableProperty] private bool _autoGenerateSku;
    [ObservableProperty] private bool _autoGenerateBarcode;
    [ObservableProperty] private string _skuPrefix = "SKU";
    [ObservableProperty] private string _skuNumberLength = "6";
    [ObservableProperty] private string _internalBarcodePrefix = "20";
    [ObservableProperty] private string? _statusMessage;

    public bool ShowOutOfStockBehavior => TrackInventory;

    public InventorySettingsViewModel(IShopContextService shopContext)
    {
        _shopContext = shopContext;
    }

    public async Task LoadAsync()
    {
        var settings = await _shopContext.GetSettingsAsync();
        if (settings is null) return;

        TrackInventory = settings.TrackInventory;
        OutOfStockBehavior = settings.OutOfStockBehavior;
        DefaultLowStockThreshold = settings.DefaultLowStockThreshold.ToString();
        AutoGenerateSku = settings.AutoGenerateSku;
        AutoGenerateBarcode = settings.AutoGenerateBarcode;
        SkuPrefix = settings.SkuPrefix;
        SkuNumberLength = settings.SkuNumberLength.ToString();
        InternalBarcodePrefix = settings.InternalBarcodePrefix;
    }

    partial void OnTrackInventoryChanged(bool value) => OnPropertyChanged(nameof(ShowOutOfStockBehavior));

    [RelayCommand]
    private async Task Save()
    {
        StatusMessage = null;

        if (!int.TryParse(DefaultLowStockThreshold, out var threshold) || threshold < 0)
        {
            StatusMessage = "Default low-stock threshold must be a whole number, zero or greater.";
            return;
        }

        var skuPrefix = SkuPrefix.Trim();
        if (skuPrefix.Length is < 1 or > 12 || skuPrefix.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
        {
            StatusMessage = "SKU prefix must be 1-12 characters using letters, numbers, hyphens or underscores.";
            return;
        }

        if (!int.TryParse(SkuNumberLength, out var skuNumberLength) || skuNumberLength is < 4 or > 12)
        {
            StatusMessage = "SKU number length must be between 4 and 12 digits.";
            return;
        }

        if (InternalBarcodePrefix.Length != 2 || !InternalBarcodePrefix.All(char.IsAsciiDigit) ||
            !int.TryParse(InternalBarcodePrefix, out var barcodePrefix) || barcodePrefix is < 20 or > 29)
        {
            StatusMessage = "Generated barcode prefix must be an internal-use prefix from 20 to 29.";
            return;
        }

        var settings = await _shopContext.GetSettingsAsync() ?? new ReceiptAdmin();

        settings.TrackInventory = TrackInventory;
        settings.OutOfStockBehavior = OutOfStockBehavior;
        settings.DefaultLowStockThreshold = threshold;
        settings.AutoGenerateSku = AutoGenerateSku;
        settings.AutoGenerateBarcode = AutoGenerateBarcode;
        settings.SkuPrefix = skuPrefix;
        settings.SkuNumberLength = skuNumberLength;
        settings.InternalBarcodePrefix = InternalBarcodePrefix;

        await _shopContext.UpdateSettingsAsync(settings);

        StatusMessage = "Saved.";
    }
}
