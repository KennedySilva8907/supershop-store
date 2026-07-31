using Microsoft.AspNetCore.Mvc;
using SuperShop.Application.Catalog;

namespace SuperShop.Api.Controllers;

public class ProductQuery
{
    [FromQuery(Name = "category")] public string? Category { get; init; }
    [FromQuery(Name = "collection")] public string? Collection { get; init; }
    [FromQuery(Name = "size")] public string? Size { get; init; }
    [FromQuery(Name = "minPrice")] public decimal? MinPrice { get; init; }
    [FromQuery(Name = "maxPrice")] public decimal? MaxPrice { get; init; }
    [FromQuery(Name = "search")] public string? Search { get; init; }
    [FromQuery(Name = "sort")] public string? Sort { get; init; }
    [FromQuery(Name = "page")] public int Page { get; init; } = 1;
    [FromQuery(Name = "pageSize")] public int PageSize { get; init; } = ProductFilter.DefaultPageSize;

    public ProductFilter ToFilter() => new()
    {
        Category = Category,
        Collection = Collection,
        Size = Size,
        MinPrice = MinPrice,
        MaxPrice = MaxPrice,
        Search = Search,
        Sort = Sort?.ToLowerInvariant() switch
        {
            "price_asc" => ProductSort.PriceAscending,
            "price_desc" => ProductSort.PriceDescending,
            "name" => ProductSort.Name,
            _ => ProductSort.Newest
        },
        Page = Page,
        PageSize = PageSize
    };
}
