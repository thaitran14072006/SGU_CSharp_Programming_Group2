namespace MocBakery.Models.PublicCatalog;

public sealed record PublicCatalogQuery(string? Search = null, int? CategoryId = null,
    string Availability = "all", string Sort = "name", int Page = 1, int PageSize = 12);

public sealed record PublicCategory(int Id, string Name, int ProductCount);

// Deliberately excludes stock quantities, lots, costs, supplier and staff/customer data.
public sealed record PublicProduct(int Id, int CategoryId, string Code, string Name,
    string Category, string Unit, decimal Price, string? Description, bool HasImage, bool InStock)
{
    public string? ImageUrl => HasImage ? $"/banh/{Id}/anh" : null;
}

public sealed record PublicCatalogPage(IReadOnlyList<PublicProduct> Items, int Total, int Page, int PageSize)
{
    public int PageCount => (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record PublicProductImage(byte[] Bytes, string ContentType);
public sealed record CatalogSelection(int ProductId, string Name, decimal CurrentUnitPrice,
    int RequestedQuantity, bool CanFulfill);

// Repository-only projection. Controllers must never serialize this stock-bearing record.
public sealed record CatalogProductSnapshot(int Id, int CategoryId, string Name, string Category,
    string Unit, decimal Price, string? Description, bool HasImage, long SellableQuantity);
public sealed record CatalogSnapshotPage(IReadOnlyList<CatalogProductSnapshot> Items, int Total);

public interface IPublicCatalogRepository
{
    IReadOnlyList<PublicCategory> Categories();
    CatalogSnapshotPage Browse(PublicCatalogQuery query);
    CatalogProductSnapshot? Find(int id);
    byte[]? Image(int id);
}
