using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record VoucherInput(
    string? Code,
    VoucherDiscountType DiscountType,
    decimal DiscountValue,
    int? MaxUsageCount,
    DateTime? ExpiresAt,
    string? Notes);

public record VoucherValidationResult(
    bool IsValid,
    string? ErrorMessage,
    VoucherDiscountType DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount);

public interface IVoucherAdminService
{
    Task<List<Voucher>> GetAllVouchersAsync();
    Task<Voucher> CreateVoucherAsync(VoucherInput input);
    Task UpdateVoucherAsync(int voucherId, VoucherInput input);
    Task SetActiveAsync(int voucherId, bool isActive);
    Task DeleteVoucherAsync(int voucherId);

    // Read-only preview used by the POS cart before checkout - SaleService
    // independently re-validates and redeems atomically at sale completion,
    // the same "UI checks first, service re-checks at commit" pattern
    // ISaleService already uses for stock.
    Task<VoucherValidationResult> ValidateVoucherAsync(string code, decimal subtotal);
}