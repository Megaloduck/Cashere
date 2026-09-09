using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Cashere.Converters;

// Converts a Product.PhotoPath (relative, e.g. "products/17.jpg") into a
// loaded Bitmap for display in an Image control. Reads directly from disk on
// every conversion rather than caching - the admin/POS screens only reload
// their product lists on explicit refreshes (LoadAsync/RefreshProductsAsync),
// so this isn't a hot path, and it also means a re-uploaded photo is picked
// up automatically with no cache-invalidation plumbing needed.
public class PhotoPathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string photoPath || string.IsNullOrWhiteSpace(photoPath))
        {
            return null;
        }

        try
        {
            var fullPath = GetFullPath(photoPath);
            return File.Exists(fullPath) ? new Bitmap(fullPath) : null;
        }
        catch
        {
            // A corrupt/partially-written file shouldn't crash the product
            // list - just fall back to the no-photo placeholder.
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    // Mirrors the media root CashereServerHost derives from the SQLite db
    // path (a sibling "media" folder under %LocalAppData%\Cashere) - kept as
    // simple path math here rather than referencing Cashere.Data, since this
    // shared project is deliberately kept EF-free.
    public static string GetFullPath(string relativePhotoPath)
    {
        var mediaRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cashere", "media");
        return Path.Combine(mediaRoot, relativePhotoPath);
    }
}