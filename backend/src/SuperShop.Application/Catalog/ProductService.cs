using SuperShop.Application.Common;
using SuperShop.Domain.Exceptions;

namespace SuperShop.Application.Catalog;

public class ProductService(ICatalogRepository repository)
{
    public Task<PagedResult<ProductListDto>> GetProductsAsync(
        ProductFilter filter,
        CancellationToken cancellationToken = default) =>
        repository.GetProductsAsync(filter.Normalised(), cancellationToken);

    public async Task<ProductDetailDto> GetProductAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var product = await repository.GetProductBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken);

        return product ?? throw NotFoundException.For("Produto", slug);
    }

    public Task<IReadOnlyList<ProductListDto>> GetFeaturedAsync(CancellationToken cancellationToken = default) =>
        repository.GetFeaturedProductsAsync(cancellationToken);

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        repository.GetCategoriesAsync(cancellationToken);

    public Task<IReadOnlyList<CollectionDto>> GetCollectionsAsync(CancellationToken cancellationToken = default) =>
        repository.GetCollectionsAsync(cancellationToken);

    public Task<IReadOnlyList<SizeDto>> GetSizesAsync(
        string? sizeSystem,
        CancellationToken cancellationToken = default) =>
        repository.GetSizesAsync(sizeSystem, cancellationToken);
}
