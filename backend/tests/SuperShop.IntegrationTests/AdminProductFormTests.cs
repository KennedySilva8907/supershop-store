using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class AdminProductFormTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task A_new_product_comes_back_with_every_size_at_zero_stock()
    {
        var admin = await factory.SignInAsAdminAsync();
        var slug = $"teste-{Guid.NewGuid():N}"[..20];

        var created = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "SuperShop Teste",
            slug,
            description = "Feito por um teste.",
            price = 49.90m,
            compareAtPrice = (decimal?)null,
            categoryId = 1,
            collectionId = 1,
            isFeatured = false
        });

        created.EnsureSuccessStatusCode();

        var product = await created.Content.ReadFromJsonAsync<Created>();
        var variants = await admin.GetFromJsonAsync<List<Variant>>($"/api/admin/products/{product!.Id}/variants");

        Assert.NotEmpty(variants!);
        Assert.All(variants!, v => Assert.Equal(0, v.Stock));
    }

    [Fact]
    public async Task The_form_reads_back_what_was_saved()
    {
        var admin = await factory.SignInAsAdminAsync();
        var slug = $"leitura-{Guid.NewGuid():N}"[..20];

        var created = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "SuperShop Leitura",
            slug,
            description = "Descrição original.",
            price = 30m,
            compareAtPrice = 45m,
            categoryId = 2,
            collectionId = 2,
            isFeatured = true
        });

        var id = (await created.Content.ReadFromJsonAsync<Created>())!.Id;

        var form = await admin.GetFromJsonAsync<Form>($"/api/admin/products/{id}");

        Assert.Equal("SuperShop Leitura", form!.Name);
        Assert.Equal("Descrição original.", form.Description);
        Assert.Equal(30m, form.Price);
        Assert.Equal(45m, form.CompareAtPrice);
        Assert.Equal(2, form.CategoryId);
        Assert.Equal(2, form.CollectionId);
        Assert.True(form.IsFeatured);
        Assert.True(form.IsActive);
    }

    [Fact]
    public async Task Editing_changes_what_the_form_reads_back()
    {
        var admin = await factory.SignInAsAdminAsync();
        var slug = $"editar-{Guid.NewGuid():N}"[..20];

        var created = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Antes",
            slug,
            description = "Antes.",
            price = 10m,
            compareAtPrice = (decimal?)null,
            categoryId = 1,
            collectionId = 1,
            isFeatured = false
        });

        var id = (await created.Content.ReadFromJsonAsync<Created>())!.Id;

        await admin.PutAsJsonAsync($"/api/admin/products/{id}", new
        {
            name = "Depois",
            slug,
            description = "Depois.",
            price = 20m,
            compareAtPrice = 25m,
            categoryId = 1,
            collectionId = 1,
            isFeatured = true
        });

        var form = await admin.GetFromJsonAsync<Form>($"/api/admin/products/{id}");

        Assert.Equal("Depois", form!.Name);
        Assert.Equal(20m, form.Price);
        Assert.True(form.IsFeatured);
    }

    [Fact]
    public async Task A_slug_that_belongs_to_another_product_is_refused()
    {
        var admin = await factory.SignInAsAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Cópia",
            slug = "axis-runner",
            description = "Devia falhar.",
            price = 10m,
            compareAtPrice = (decimal?)null,
            categoryId = 1,
            collectionId = 1,
            isFeatured = false
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_product_that_does_not_exist_is_a_not_found()
    {
        var admin = await factory.SignInAsAdminAsync();

        var response = await admin.GetAsync("/api/admin/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_open_the_form()
    {
        var customer = await factory.SignInAsCustomerAsync($"curioso-{Guid.NewGuid():N}@supershop.pt");

        var response = await customer.GetAsync("/api/admin/products/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private record Created(int Id);

    private record Variant(int Id, int Stock);

    private record Form(
        int Id,
        string Name,
        string Slug,
        string Description,
        decimal Price,
        decimal? CompareAtPrice,
        int CategoryId,
        int CollectionId,
        bool IsActive,
        bool IsFeatured);
}
