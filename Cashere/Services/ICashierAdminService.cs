using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

// Password is nullable so an update can leave the existing hash untouched -
// required (non-empty) only when creating a new cashier.
public record CashierInput(string Username, string DisplayName, UserRole Role, string? Password);

public interface ICashierAdminService
{
    Task<List<Cashier>> GetAllCashiersAsync();
    Task<Cashier> CreateCashierAsync(CashierInput input);
    Task UpdateCashierAsync(int cashierId, CashierInput input);
    Task SetActiveAsync(int cashierId, bool isActive);
}