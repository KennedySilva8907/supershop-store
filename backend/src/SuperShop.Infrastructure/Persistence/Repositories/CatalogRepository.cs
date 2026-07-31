using Microsoft.EntityFrameworkCore;
using SuperShop.Application.Catalog;
using SuperShop.Application.Common;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;

namespace SuperShop.Infrastructure.Persistence.Repositories;

public class CatalogRepository(SuperShopDbContext context) : ICatalogRepository
{
    public async Task<PagedResult<ProductListDto>> GetProductsAsync(
        ProductFilter filter,
        CancellationToken cancellationToken)
    {
        var query = Active();

        if (filter.Category is not null)
        {
            query = query.Where(p => p.Category.Slug == filter.Category);
        }

        var collections = filter.CollectionSlugs;
        if (collections.Count > 0)
        {
            query = query.Where(p => collections.Contains(p.Collection.Slug));
        }

        if (filter.Size is not null)
        {
            query = query.Where(p => p.Variants.Any(v => v.Size.Label == filter.Size && v.Stock > 0));
        }

        if (filter.MinPrice is not null)
        {
            query = query.Where(p => p.Price >= filter.MinPrice);
        }

        if (filter.MaxPrice is not null)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice);
        }

        if (filter.Search is not null)
        {
            var term = $"%{filter.Search}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Description, term));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        query = filter.Sort switch
        {
            ProductSort.PriceAscending => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            ProductSort.PriceDescending => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            ProductSort.Name => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(ToListDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListDto>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken) =>
        await Active()
            .Where(p => p.Slug == slug)
            .Select(p => new ProductDetailDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                p.Price,
                p.CompareAtPrice,
                p.Category.Name,
                p.Category.Slug,
                p.Category.SizeSystem.ToString(),
                p.Collection.Name,
                p.Collection.Slug,
                p.IsFeatured,
                p.Variants
                    .OrderBy(v => v.Size.SortOrder)
                    .Select(v => new ProductVariantDto(v.Id, v.Size.Label, v.Size.SortOrder, v.Sku, v.Stock))
                    .ToList(),
                p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new ProductImageDto(i.PublicId, i.AltText, i.IsPrimary, i.SortOrder))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductListDto>> GetFeaturedProductsAsync(CancellationToken cancellationToken) =>
        await Active()
            .Where(p => p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Select(ToListDto)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.SizeSystem.ToString(), c.DisplayOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CollectionDto>> GetCollectionsAsync(CancellationToken cancellationToken) =>
        await context.Collections
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CollectionDto(c.Id, c.Name, c.Slug))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SizeDto>> GetSizesAsync(string? sizeSystem, CancellationToken cancellationToken)
    {
        var query = context.Sizes.AsNoTracking();

        if (Enum.TryParse<SizeSystem>(sizeSystem, ignoreCase: true, out var system))
        {
            query = query.Where(s => s.SizeSystem == system);
        }

        return await query
            .OrderBy(s => s.SizeSystem)
            .ThenBy(s => s.SortOrder)
            .Select(s => new SizeDto(s.Id, s.SizeSystem.ToString(), s.Label, s.SortOrder))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Product> Active() =>
        context.Products.AsNoTracking().Where(p => p.IsActive);

    private static readonly System.Linq.Expressions.Expression<Func<Product, ProductListDto>> ToListDto =
        p => new ProductListDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Price,
            p.CompareAtPrice,
            p.Category.Name,
            p.Category.Slug,
            p.Collection.Name,
            p.IsFeatured,
            p.Variants.Any(v => v.Stock > 0),
            p.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => new ProductImageDto(i.PublicId, i.AltText, i.IsPrimary, i.SortOrder))
                .FirstOrDefault());
}
