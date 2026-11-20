using System.Globalization;
using MocBakery.Models.PublicCatalog;
namespace MocBakery.Web.Models;

public sealed record CatalogViewModel(PublicCatalogPage Products, IReadOnlyList<PublicCategory> Categories,
    PublicCatalogQuery Query, bool IsHome = false);

public static class BakeryDisplay
{
    public static string Money(decimal price) => price.ToString(price == decimal.Truncate(price) ? "N0" : "N2", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";

    // A desktop-uploaded image always takes priority over the supplied type photos.
    public static string? Photo(PublicProduct product)
    {
        if (product.HasImage) return product.ImageUrl;
        string name = product.Name.ToLowerInvariant();
        string? file = name.Contains("cookie") || name.Contains("bánh quy") ? "cookie.jpg"
            : name.Contains("muffin") ? "muffin.jpg"
            : name.Contains("croissant") || name.Contains("sừng bò") ? "croissant.jpg"
            : name.Contains("tiramisu") ? "tiramisu.jpg"
            : name.Contains("canelé") || name.Contains("canele") ? "canele.png"
            : name.Contains("madeleine") ? "madeleine.png"
            : name.Contains("financier") ? "financier.png"
            : name.Contains("macaron") ? "macaron.png"
            : name.Contains("éclair") || name.Contains("eclair") ? "eclair.png" : null;
        return file is null ? null : $"/images/bakery/{file}";
    }
}
