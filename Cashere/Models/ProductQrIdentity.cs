using System;
using System.Globalization;

namespace Cashere.Models;

/// <summary>A stable, Cashere-specific scan identity derived from a product's database id.</summary>
public static class ProductQrIdentity
{
    private const string Prefix = "cashere://product/";

    public static string ForProductId(int productId) =>
        productId > 0 ? Prefix + productId.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public static bool TryGetProductId(string? value, out int productId)
    {
        productId = 0;
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = value[Prefix.Length..];
        return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out productId) && productId > 0;
    }
}
