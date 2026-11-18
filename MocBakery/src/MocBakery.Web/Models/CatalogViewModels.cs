using System.Globalization;
using MocBakery.Models.PublicCatalog;
namespace MocBakery.Web.Models;

public sealed record CatalogViewModel(PublicCatalogPage Products, IReadOnlyList<PublicCategory> Categories,
    PublicCatalogQuery Query, bool IsHome = false);

public static class BakeryDisplay
{
    public static string Money(decimal price) => price.ToString(price == decimal.Truncate(price) ? "N0" : "N2", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
}
