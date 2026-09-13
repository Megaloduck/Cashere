using Cashere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services
{
    public interface IShopContextService
    {
        Task<Cashier?> GetDefaultCashierAsync();
        Task<decimal> GetTaxRatePercentAsync();
        Task<string> GetShopNameAsync();
        Task<ReceiptAdmin?> GetSettingsAsync();
        Task UpdateSettingsAsync(ReceiptAdmin settings);
    }
}