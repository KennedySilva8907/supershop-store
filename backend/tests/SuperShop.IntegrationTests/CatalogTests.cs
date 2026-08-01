using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class CatalogTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task Catalogue_filters_return_what_is_expected()
    {
        var client = factory.CreateClient();

        var all = await client.GetFromJsonAsync<Paged>("/api/products?pageSize=48");
        var footwear = await client.GetFromJsonAsync<Paged>("/api/products?category=sapatilhas&pageSize=48");
        var axis = await client.GetFromJsonAsync<Paged>("/api/products?collection=axis&pageSize=48");

        Assert.Equal(40, all!.TotalItems);
        Assert.Equal(10, footwear!.TotalItems);
        Assert.Equal(17, axis!.TotalItems);
        Assert.All(footwear.Items, p => Assert.Equal("sapatilhas", p.CategorySlug));
    }

    [Fact]
    public async Task Page_size_is_capped_so_a_huge_request_cannot_take_the_api_down()
    {
        var client = factory.CreateClient();

        var page = await client.GetFromJsonAsync<Paged>("/api/products?pageSize=100000");

        Assert.Equal(48, page!.PageSize);
    }

    [Fact]
    public async Task Search_ignores_case()
    {
        var client = factory.CreateClient();

        var lower = await client.GetFromJsonAsync<Paged>("/api/products?search=casaco&pageSize=48");
        var upper = await client.GetFromJsonAsync<Paged>("/api/products?search=CASACO&pageSize=48");

        Assert.Equal(lower!.TotalItems, upper!.TotalItems);
        Assert.True(lower.TotalItems > 0);
    }

    [Fact]
    public async Task An_unknown_product_returns_not_found_as_problem_json()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private record Paged(int TotalItems, int PageSize, List<Item> Items);

    private record Item(string Slug, string CategorySlug);
}
