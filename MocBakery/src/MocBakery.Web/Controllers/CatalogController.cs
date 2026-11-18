using Microsoft.AspNetCore.Mvc;
using MocBakery.BLL.Services.Catalog;
using MocBakery.Models;
using MocBakery.Models.PublicCatalog;
using MocBakery.Web.Models;
namespace MocBakery.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CatalogController(PublicCatalogService catalog) : Controller
{
    [HttpGet("/")]
    public IActionResult Home() => View("Browse", new CatalogViewModel(catalog.Browse(new(PageSize: 6)), catalog.Categories(), new(), true));

    [HttpGet("/banh")]
    [HttpGet("/danh-muc/{categoryId:int}")]
    public IActionResult Browse(string? q, int? categoryId, string availability = "all", string sort = "name", int page = 1)
    {
        if (!ModelState.IsValid) return InvalidFilters();
        try
        {
            var query = new PublicCatalogQuery(q, categoryId, availability, sort, page);
            var categories = catalog.Categories();
            if (categoryId is not null && !categories.Any(c => c.Id == categoryId)) return Missing();
            return View(new CatalogViewModel(catalog.Browse(query), categories, query));
        }
        catch (BusinessException) { return InvalidFilters(); }
    }

    [HttpGet("/danh-muc")]
    public IActionResult Categories() => View(catalog.Categories());

    [HttpGet("/banh/{id:int}")]
    public IActionResult Detail(int id) => catalog.Find(id) is { } product ? View(product) : Missing();

    [HttpGet("/banh/{id:int}/anh")]
    public IActionResult Image(int id) => catalog.Image(id) is { } image ? File(image.Bytes, image.ContentType) : NotFound();

    private IActionResult Missing() { Response.StatusCode = 404; ViewData["Title"] = "Không tìm thấy bánh"; return View("CatalogMessage", "Bánh hoặc danh mục này hiện không được mở bán. Bạn có thể chọn bánh khác trong thực đơn."); }
    private IActionResult InvalidFilters() { Response.StatusCode = 400; ViewData["Title"] = "Bộ lọc chưa hợp lệ"; return View("CatalogMessage", "Vui lòng kiểm tra từ khóa (tối đa 100 ký tự), danh mục, bộ lọc và số trang rồi thử lại."); }
}

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicCatalogApiController(PublicCatalogService catalog) : ControllerBase
{
    [HttpGet("/api/danh-muc")]
    public IActionResult Categories() => Ok(catalog.Categories());

    [HttpGet("/api/banh")]
    public IActionResult Browse(string? q, int? categoryId, string availability = "all", string sort = "name", int page = 1, int pageSize = 12)
    {
        try { return Ok(catalog.Browse(new(q, categoryId, availability, sort, page, pageSize))); }
        catch (BusinessException) { return Problem(statusCode: 400, title: "Bộ lọc không hợp lệ."); }
    }

    [HttpGet("/api/banh/{id:int}")]
    public IActionResult Detail(int id) => catalog.Find(id) is { } product ? Ok(product) : NotFound();
}
