using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// Thrown by the admin CRUD services (products, categories, suppliers) when
// input fails a business rule - e.g. a duplicate SKU or a supplier with
// purchase history. The ViewModel catches this and surfaces the message
// directly, same pattern as InsufficientStockException in ISaleService.
public class AdminValidationException : Exception
{
    public AdminValidationException(string message) : base(message) { }
}