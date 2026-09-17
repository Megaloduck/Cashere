using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class VoucherAdminService : IVoucherAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public VoucherAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Voucher>> GetAllVouchersAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Vouchers.OrderByDescending(v => v.CreatedAt).AsNoTracking().ToListAsync();
    }

    public async Task<Voucher> CreateVoucherAsync(VoucherInput input)
    {
        ValidateDiscountValue(input);

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var code = string.IsNullOrWhiteSpace(input.Code)
            ? await GenerateUniqueCodeAsync(db)
            : input.Code.Trim().ToUpperInvariant();

        if (await db.Vouchers.AnyAsync(v => v.Code == code))
        {
            throw new AdminValidationException($"Voucher code '{code}' is already in use.");
        }

        var voucher = new Voucher
        {
            Code = code,
            DiscountType = input.DiscountType,
            DiscountValue = input.DiscountValue,
            MaxUsageCount = input.MaxUsageCount,
            ExpiresAt = input.ExpiresAt,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Vouchers.Add(voucher);
        await db.SaveChangesAsync();
        return voucher;
    }

    public async Task UpdateVoucherAsync(int voucherId, VoucherInput input)
    {
        ValidateDiscountValue(input);

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId)
            ?? throw new AdminValidationException($"Voucher {voucherId} was not found.");

        // Code is intentionally not editable here - changing it after it may
        // already be printed/shared would silently break redemption for
        // anyone holding the original code.
        voucher.DiscountType = input.DiscountType;
        voucher.DiscountValue = input.DiscountValue;
        voucher.MaxUsageCount = input.MaxUsageCount;
        voucher.ExpiresAt = input.ExpiresAt;
        voucher.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int voucherId, bool isActive)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId)
            ?? throw new AdminValidationException($"Voucher {voucherId} was not found.");

        voucher.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task DeleteVoucherAsync(int voucherId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId);
        if (voucher is null) return;

        try
        {
            db.Vouchers.Remove(voucher);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new AdminValidationException(
                $"'{voucher.Code}' can't be deleted because it has already been redeemed - deactivate it instead.");
        }
    }

    public async Task<VoucherValidationResult> ValidateVoucherAsync(string code, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Invalid("Enter a voucher code.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var normalizedCode = code.Trim().ToUpperInvariant();
        var voucher = await db.Vouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Code == normalizedCode);

        if (voucher is null) return Invalid("That voucher code doesn't exist.");
        if (!voucher.IsActive) return Invalid("This voucher is no longer active.");
        if (voucher.ExpiresAt is not null && voucher.ExpiresAt < DateTime.UtcNow) return Invalid("This voucher has expired.");
        if (voucher.MaxUsageCount is int max && voucher.UsageCount >= max) return Invalid("This voucher has already been fully redeemed.");

        var discountAmount = ComputeDiscount(voucher.DiscountType, voucher.DiscountValue, subtotal);
        return new VoucherValidationResult(true, null, voucher.DiscountType, voucher.DiscountValue, discountAmount);

        static VoucherValidationResult Invalid(string message) =>
            new(false, message, VoucherDiscountType.FixedAmount, 0, 0);
    }

    // Shared with SaleService's own server-side re-validation, so a
    // percentage voucher's Rp value is always computed the same way
    // whether it's a UI preview or the authoritative redemption.
    public static decimal ComputeDiscount(VoucherDiscountType type, decimal value, decimal subtotal) =>
        type == VoucherDiscountType.Percentage
            ? Math.Round(subtotal * (value / 100m), 2, MidpointRounding.AwayFromZero)
            : Math.Min(value, subtotal);

    private static void ValidateDiscountValue(VoucherInput input)
    {
        if (input.DiscountValue <= 0)
        {
            throw new AdminValidationException("Discount value must be greater than zero.");
        }
        if (input.DiscountType == VoucherDiscountType.Percentage && input.DiscountValue > 100)
        {
            throw new AdminValidationException("A percentage voucher can't discount more than 100%.");
        }
    }

    // Six characters from an unambiguous alphabet (no 0/O, 1/I) - short
    // enough to read aloud or type at a till, same "internal use, no
    // external-registry uniqueness needed" spirit as
    // ProductAdminService.GenerateEan13InternalUseBarcode.
    private static async Task<string> GenerateUniqueCodeAsync(CashereDbContext db)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var sb = new StringBuilder(6);
            for (var i = 0; i < 6; i++)
            {
                sb.Append(alphabet[random.Next(alphabet.Length)]);
            }

            var candidate = sb.ToString();
            if (!await db.Vouchers.AnyAsync(v => v.Code == candidate))
            {
                return candidate;
            }
        }

        return $"V{DateTime.UtcNow:yyMMddHHmmssfff}";
    }
}