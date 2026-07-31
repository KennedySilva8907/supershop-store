namespace SuperShop.Application.Catalog;

public enum ProductSort
{
    Newest = 0,
    PriceAscending = 1,
    PriceDescending = 2,
    Name = 3
}

public class ProductFilter
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 48;

    public string? Category { get; init; }
    public string? Collection { get; init; }
    public string? Size { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Search { get; init; }
    public ProductSort Sort { get; init; } = ProductSort.Newest;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;

    public IReadOnlyList<string> CollectionSlugs =>
        Collection?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Distinct()
            .ToArray() ?? [];

    public ProductFilter Normalised() => new()
    {
        Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim().ToLowerInvariant(),
        Collection = Collection,
        Size = string.IsNullOrWhiteSpace(Size) ? null : Size.Trim().ToUpperInvariant(),
        MinPrice = MinPrice,
        MaxPrice = MaxPrice,
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        Sort = Sort,
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        }
    };
}
