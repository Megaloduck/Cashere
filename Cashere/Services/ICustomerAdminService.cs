using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public record CustomerInput(string Name, string? Phone, string? Email, string? Address, string? Notes);

public interface ICustomerAdminService
{
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer> CreateCustomerAsync(CustomerInput input);
    Task UpdateCustomerAsync(int customerId, CustomerInput input);
    Task DeleteCustomerAsync(int customerId);
}