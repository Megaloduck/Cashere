using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    public record PaymentSettings(
        bool CashEnabled,
        bool QrisEnabled,
        bool EdcEnabled,
        string? QrisAccountInfo,
        string? EdcAccountInfo,
        bool RequireConfirmationForNonCash,
        decimal CashFeePercent,
        decimal QrisFeePercent,
        decimal EdcFeePercent)
    {
        public bool IsMethodEnabled(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => CashEnabled,
            PaymentMethod.Qris => QrisEnabled,
            PaymentMethod.Edc => EdcEnabled,
            _ => true
        };

        public string? AccountInfoFor(PaymentMethod method) => method switch
        {
            PaymentMethod.Qris => QrisAccountInfo,
            PaymentMethod.Edc => EdcAccountInfo,
            _ => null
        };

        // Settings -> Payments -> per-method fee. Used identically by
        // CheckoutViewModel's live preview and SaleService's authoritative
        // recomputation (never trusted from the client) to work out
        // Payment.FeeAmount - see TaxCalculator-style "one function, two
        // callers" split, just for payment fees instead of tax.
        public decimal FeePercentFor(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash => CashFeePercent,
            PaymentMethod.Qris => QrisFeePercent,
            PaymentMethod.Edc => EdcFeePercent,
            _ => 0
        };

        // The one place a fee amount is computed - CheckoutViewModel calls
        // it for the live preview and SaleService calls it to record the
        // authoritative Payment.FeeAmount, so the two can never round
        // differently.
        public decimal ComputeFee(PaymentMethod method, decimal amount) =>
            Math.Round(amount * (FeePercentFor(method) / 100m), 2, MidpointRounding.AwayFromZero);
    }

    // Read by CheckoutViewModel/SaleService (RequireCustomerBeforeCheckout)
    // and PosViewModel (AutoPrintReceiptAfterPayment) - see Settings ->
    // Sales Behavior.
    public record SalesBehaviorSettings(
        bool RequireCustomerBeforeCheckout,
        bool AutoPrintReceiptAfterPayment);

    // Read by RootViewModel right after login (to arm AutoLockService) and
    // by SecuritySettingsViewModel (to populate the toggle/picker) - see
    // Settings -> Security.
    public record SecuritySettings(bool AutoLockEnabled, int AutoLockTimeoutMinutes);

    // Read by RootViewModel right after login (to arm HeaderClockService)
    // and by PreferencesViewModel (to populate the toggles) - see
    // Settings -> Preferences.
    public record HeaderClockSettings(
        bool IsVisible, bool ShowDay, bool ShowDate, bool ShowMonth, bool ShowYear, bool ShowHours);

    // Read by RootViewModel right after login (to seed CartViewModel) and by
    // PosViewModel.RefreshTaxSettingsAsync (so a Business Info change takes
    // effect the moment the cashier returns to the till, no restart needed) -
    // see Settings -> Business Info -> Tax Rates / Rounding.
    public record TaxAndRoundingSettings(
        decimal DefaultTaxRatePercent,
        bool PricesIncludeTax,
        RoundingMode RoundingMode,
        decimal RoundingIncrement);

    public interface IShopContextService
    {
        Task<Cashier?> GetDefaultCashierAsync();
        Task<decimal> GetTaxRatePercentAsync();
        Task<string> GetShopNameAsync();
        Task<ReceiptAdmin?> GetSettingsAsync();
        Task UpdateSettingsAsync(ReceiptAdmin settings);
        Task<PaymentSettings> GetPaymentSettingsAsync();
        Task<SalesBehaviorSettings> GetSalesBehaviorSettingsAsync();
        Task<SecuritySettings> GetSecuritySettingsAsync();
        Task<HeaderClockSettings> GetHeaderClockSettingsAsync();
        Task<TaxAndRoundingSettings> GetTaxAndRoundingSettingsAsync();
    }
}