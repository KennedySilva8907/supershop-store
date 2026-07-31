using Microsoft.AspNetCore.Mvc;
using SuperShop.Application.Catalog;
using SuperShop.Application.Common;

namespace SuperShop.Api.Controllers;

[ApiController]
[Route("api")]
public class CatalogController(ProductService products) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await products.GetCategoriesAsync(cancellationToken));

    [HttpGet("collections")]
    public async Task<ActionResult<IReadOnlyList<CollectionDto>>> GetCollections(CancellationToken cancellationToken) =>
        Ok(await products.GetCollectionsAsync(cancellationToken));

    [HttpGet("sizes")]
    public async Task<ActionResult<IReadOnlyList<SizeDto>>> GetSizes(
        [FromQuery] string? sizeSystem,
        CancellationToken cancellationToken) =>
        Ok(await products.GetSizesAsync(sizeSystem, cancellationToken));

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken) =>
        Ok(await products.GetProductsAsync(query.ToFilter(), cancellationToken));

    [HttpGet("products/featured")]
    public async Task<ActionResult<IReadOnlyList<ProductListDto>>> GetFeatured(CancellationToken cancellationToken) =>
        Ok(await products.GetFeaturedAsync(cancellationToken));

    [HttpGet("products/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(
        string slug,
        CancellationToken cancellationToken) =>
        Ok(await products.GetProductAsync(slug, cancellationToken));
}
