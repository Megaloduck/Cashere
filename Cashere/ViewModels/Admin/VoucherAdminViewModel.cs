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

public partial class VoucherAdminViewModel : ViewModelBase
{
    private readonly IVoucherAdminService? _voucherAdmin;
    private readonly UserRole _currentRole;
    private int? _editingVoucherId;

    public ObservableCollection<Voucher> Vouchers { get; } = new();
    public IReadOnlyList<VoucherDiscountType> DiscountTypes { get; } = Enum.GetValues<VoucherDiscountType>();

    public bool IsServiceAvailable => _voucherAdmin is not null;

    // Creating discount codes is financially sensitive, same bar as
    // Products/Sales History - view-only for Cashier logins.
    public bool CanManage => IsServiceAvailable && RolePermissions.CanManage(_currentRole);

    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string _editorTitle = "NEW VOUCHER";
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _formCode = string.Empty;
    [ObservableProperty] private VoucherDiscountType _formDiscountType = VoucherDiscountType.FixedAmount;
    [ObservableProperty] private string _formDiscountValue = "0";
    [ObservableProperty] private string _formMaxUsageCount = string.Empty;
    [ObservableProperty] private DateTimeOffset? _formExpiresAt;
    [ObservableProperty] private string _formNotes = string.Empty;

    public bool IsEditingExistingVoucher => _editingVoucherId is not null;

    public VoucherAdminViewModel(IVoucherAdminService? voucherAdmin, UserRole currentRole)
    {
        _voucherAdmin = voucherAdmin;
        _currentRole = currentRole;
    }

    public async Task LoadAsync()
    {
        if (_voucherAdmin is null) return;

        var vouchers = await _voucherAdmin.GetAllVouchersAsync();
        Vouchers.Clear();
        foreach (var voucher in vouchers) Vouchers.Add(voucher);
    }

    [RelayCommand]
    private void AddNew()
    {
        if (!CanManage) return;

        _editingVoucherId = null;
        EditorTitle = "NEW VOUCHER";
        FormCode = string.Empty;
        FormDiscountType = VoucherDiscountType.FixedAmount;
        FormDiscountValue = "0";
        FormMaxUsageCount = string.Empty;
        FormExpiresAt = null;
        FormNotes = string.Empty;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsEditingExistingVoucher));
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditVoucher(Voucher? voucher)
    {
        if (!CanManage || voucher is null) return;

        _editingVoucherId = voucher.Id;
        EditorTitle = "EDIT VOUCHER";
        FormCode = voucher.Code;
        FormDiscountType = voucher.DiscountType;
        FormDiscountValue = voucher.DiscountValue.ToString();
        FormMaxUsageCount = voucher.MaxUsageCount?.ToString() ?? string.Empty;
        FormExpiresAt = voucher.ExpiresAt.HasValue ? new DateTimeOffset(voucher.ExpiresAt.Value) : null;
        FormNotes = voucher.Notes ?? string.Empty;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsEditingExistingVoucher));
        IsEditorOpen = true;
    }

    [RelayCommand]
    private async Task ToggleActive(Voucher? voucher)
    {
        if (!CanManage || voucher is null || _voucherAdmin is null) return;
        await _voucherAdmin.SetActiveAsync(voucher.Id, !voucher.IsActive);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteVoucher(Voucher? voucher)
    {
        if (!CanManage || voucher is null || _voucherAdmin is null) return;

        try
        {
            await _voucherAdmin.DeleteVoucherAsync(voucher.Id);
            await LoadAsync();
        }
        catch (AdminValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanManage || _voucherAdmin is null) return;

        ErrorMessage = null;

        if (!decimal.TryParse(FormDiscountValue, out var discountValue) || discountValue <= 0)
        {
            ErrorMessage = "Discount value must be a number greater than zero.";
            return;
        }

        int? maxUsageCount = null;
        if (!string.IsNullOrWhiteSpace(FormMaxUsageCount))
        {
            if (!int.TryParse(FormMaxUsageCount, out var parsed) || parsed <= 0)
            {
                ErrorMessage = "Usage limit must be a whole number greater than zero, or left blank for unlimited.";
                return;
            }
            maxUsageCount = parsed;
        }

        var input = new VoucherInput(
            string.IsNullOrWhiteSpace(FormCode) ? null : FormCode.Trim(),
            FormDiscountType,
            discountValue,
            maxUsageCount,
            FormExpiresAt?.UtcDateTime,
            string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes);

        try
        {
            if (_editingVoucherId is int id)
            {
                await _voucherAdmin.UpdateVoucherAsync(id, input);
            }
            else
            {
                await _voucherAdmin.CreateVoucherAsync(input);
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