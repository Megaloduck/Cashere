using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    // Read by CheckoutViewModel/SaleService (RequireCustomerBeforeCheckout)
    // and PosViewModel (AutoPrintReceiptAfterPayment) - see Settings ->
    // Sales Behavior.
    public record SalesBehaviorSettings(
        bool RequireCustomerBeforeCheckout,
        bool AutoPrintReceiptAfterPayment);

    public interface IShopContextService
    {
        Task<Cashier?> GetDefaultCashierAsync();
        Task<decimal> GetTaxRatePercentAsync();
        Task<string> GetShopNameAsync();
        Task<ReceiptAdmin?> GetSettingsAsync();
        Task UpdateSettingsAsync(ReceiptAdmin settings);
        Task<PaymentSettings> GetPaymentSettingsAsync();
        Task<SalesBehaviorSettings> GetSalesBehaviorSettingsAsync();
    }
}