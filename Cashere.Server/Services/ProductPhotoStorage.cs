using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Cashere.Server.Services;

// Resolves and ensures the on-disk folder product photos are saved to and
// served from. Root is a sibling of the SQLite db file so a fresh install
// gets media storage automatically without extra configuration - see
// CashereServerHost.StartAsync for how mediaRootFolder is derived.
public class ProductPhotoStorage
{
    public string ProductsFolder { get; }

    public ProductPhotoStorage(string mediaRootFolder)
    {
        ProductsFolder = Path.Combine(mediaRootFolder, "products");
        Directory.CreateDirectory(ProductsFolder);
    }
}