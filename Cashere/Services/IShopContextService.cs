using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    // Enabled/disabled state and cashier-facing hints for each payment
    // method - read by CheckoutViewModel and re-checked by SaleService at
    // completion. Deliberately excludes fees/refunds/partial payment - none
    // of those have a checkout flow to configure yet.
    public record PaymentSettings(
        bool CashEnabled,
        bool QrisEnabled,
        bool EdcEnabled,
        string? QrisAccountInfo,
        string? EdcAccountInfo,
        bool RequireConfirmationForNonCash)
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
    }

    public interface IShopContextService
    {
        Task<Cashier?> GetDefaultCashierAsync();
        Task<decimal> GetTaxRatePercentAsync();
        Task<string> GetShopNameAsync();
        Task<ReceiptAdmin?> GetSettingsAsync();
        Task UpdateSettingsAsync(ReceiptAdmin settings);
        Task<PaymentSettings> GetPaymentSettingsAsync();
    }
}