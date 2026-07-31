namespace SuperShop.Application.Catalog;

public record CategoryDto(int Id, string Name, string Slug, string SizeSystem, int DisplayOrder);

public record CollectionDto(int Id, string Name, string Slug);

public record SizeDto(int Id, string SizeSystem, string Label, int SortOrder);

public record ProductImageDto(string PublicId, string AltText, bool IsPrimary, int SortOrder);

public record ProductVariantDto(int Id, string SizeLabel, int SizeSortOrder, string Sku, int Stock)
{
    public bool IsInStock => Stock > 0;
    public bool IsLowStock => Stock > 0 && Stock < 5;
}

public record ProductListDto(
    int Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    string CategoryName,
    string CategorySlug,
    string CollectionName,
    bool IsFeatured,
    bool HasStock,
    ProductImageDto? PrimaryImage)
{
    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
}

public record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? CompareAtPrice,
    string CategoryName,
    string CategorySlug,
    string SizeSystem,
    string CollectionName,
    string CollectionSlug,
    bool IsFeatured,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images)
{
    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
    public bool HasStock => Variants.Any(v => v.Stock > 0);
}
