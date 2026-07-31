using SuperShop.Application.Common;

namespace SuperShop.Application.Catalog;

public interface ICatalogRepository
{
    Task<PagedResult<ProductListDto>> GetProductsAsync(ProductFilter filter, CancellationToken cancellationToken);

    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductListDto>> GetFeaturedProductsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CollectionDto>> GetCollectionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SizeDto>> GetSizesAsync(string? sizeSystem, CancellationToken cancellationToken);
}
