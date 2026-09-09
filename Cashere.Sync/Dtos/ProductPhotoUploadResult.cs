    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Sync.Dtos
{
    // Returned after a successful photo upload so the mobile client can show a
    // confirmation without needing a second round trip to fetch the product.
    public record ProductPhotoUploadResult(string PhotoPath, string PhotoUrl);
}
