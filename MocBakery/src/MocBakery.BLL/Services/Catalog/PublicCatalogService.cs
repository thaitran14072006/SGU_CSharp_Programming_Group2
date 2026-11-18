using MocBakery.Models;
using MocBakery.Models.PublicCatalog;
namespace MocBakery.BLL.Services.Catalog;

public sealed class PublicCatalogService(IPublicCatalogRepository repository)
{
    public IReadOnlyList<PublicCategory> Categories() => repository.Categories();

    public PublicCatalogPage Browse(PublicCatalogQuery query)
    {
        string search=query.Search?.Trim() ?? "";
        if(search.Length>100)throw new BusinessException("Từ khóa tối đa 100 ký tự.","Search");
        if(query.CategoryId is <=0)throw new BusinessException("Danh mục không hợp lệ.","CategoryId");
        if(query.Availability is not ("all" or "in-stock" or "out-of-stock"))throw new BusinessException("Bộ lọc tình trạng không hợp lệ.","Availability");
        if(query.Sort is not ("name" or "price-asc" or "price-desc"))throw new BusinessException("Thứ tự sắp xếp không hợp lệ.","Sort");
        if(query.Page<1||query.PageSize is <1 or >24)throw new BusinessException("Trang phải lớn hơn 0, mỗi trang từ 1 đến 24 bánh.","Page");
        var result=repository.Browse(query with{Search=search});
        return new(result.Items.Select(Public).ToList(),result.Total,query.Page,query.PageSize);
    }

    public PublicProduct? Find(int id) => id<=0 ? null : repository.Find(id) is { } row ? Public(row) : null;

    public CatalogSelection? CheckSelection(int id,int quantity)
    {
        if(quantity is <1 or >1000)throw new BusinessException("Số lượng phải từ 1 đến 1000.","Quantity");
        var row=id>0?repository.Find(id):null;
        return row is null?null:new(row.Id,row.Name,row.Price,quantity,row.SellableQuantity>=quantity);
    }

    public PublicProductImage? Image(int id)
    {
        if(id<=0)return null;
        byte[]? bytes=repository.Image(id);
        if(bytes is not {Length:>0}||bytes.Length>5*1024*1024)return null;
        if(bytes.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}))return new(bytes,"image/png");
        if(bytes.Length>=4&&bytes[0]==255&&bytes[1]==216&&bytes[^2]==255&&bytes[^1]==217)return new(bytes,"image/jpeg");
        return null;
    }

    private static PublicProduct Public(CatalogProductSnapshot p)=>new(p.Id,p.CategoryId,$"B{p.Id:000}",p.Name,p.Category,p.Unit,p.Price,p.Description,p.HasImage,p.SellableQuantity>0);
}
