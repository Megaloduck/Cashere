using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Cashere.Converters;

// Converts ReceiptAdmin.LogoPath (relative, e.g. "branding/logo.png") into a
// loaded Bitmap for display - the same "relative path resolved against a
// local media root" pattern as PhotoPathToImageConverter, just a different
// subfolder so a shop's logo and its product photos never collide.
public class LogoPathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string logoPath || string.IsNullOrWhiteSpace(logoPath))
        {
            return null;
        }

        try
        {
            var fullPath = GetFullPath(logoPath);
            return File.Exists(fullPath) ? new Bitmap(fullPath) : null;
        }
        catch
        {
            // A corrupt/partially-written file shouldn't crash Business
            // Info - just fall back to the no-logo placeholder.
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string GetFullPath(string relativeLogoPath)
    {
        var mediaRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cashere", "media");
        return Path.Combine(mediaRoot, relativeLogoPath);
    }

    // Saves the picked logo bytes under the media root's "branding"
    // subfolder and returns the relative path to store on ReceiptAdmin. A
    // fixed file name ("logo" + extension) means re-picking a logo simply
    // overwrites the old file instead of orphaning it on disk - same
    // one-photo-per-product cleanup philosophy as ProductPhotoEndpoints.
    public static string SaveLogo(byte[] bytes, string originalFileName)
    {
        var mediaRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cashere", "media", "branding");
        Directory.CreateDirectory(mediaRoot);

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

        foreach (var stale in Directory.GetFiles(mediaRoot, "logo.*"))
        {
            try { File.Delete(stale); } catch { /* best-effort cleanup */ }
        }

        var fileName = $"logo{ext.ToLowerInvariant()}";
        File.WriteAllBytes(Path.Combine(mediaRoot, fileName), bytes);
        return $"branding/{fileName}";
    }

    public static void DeleteLogo(string? relativeLogoPath)
    {
        if (string.IsNullOrWhiteSpace(relativeLogoPath)) return;

        try
        {
            var fullPath = GetFullPath(relativeLogoPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch
        {
        }
    }
}
