using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class StockConcurrencyTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    private const int Multibanco = 1;

    [Fact]
    public async Task Only_one_of_two_races_for_the_last_unit_can_be_paid()
    {
        var admin = await factory.SignInAsAdminAsync();
        var variantId = await LastUnitOf(admin, "axis-runner");

        var first = await AwaitingPaymentOrder(variantId);
        var second = await AwaitingPaymentOrder(variantId);

        var confirmations = await Task.WhenAll(
            first.Client.PostAsync($"/api/payments/{first.OrderNumber}/confirm", null),
            second.Client.PostAsync($"/api/payments/{second.OrderNumber}/confirm", null));

        var paid = confirmations.Count(r => r.IsSuccessStatusCode);
        var refused = confirmations.Count(r => !r.IsSuccessStatusCode);

        Assert.Equal(1, paid);
        Assert.Equal(1, refused);
        Assert.Equal(0, await StockOf(admin, variantId));
    }

    [Fact]
    public async Task Five_confirming_at_once_sell_one_unit_and_no_more()
    {
        var admin = await factory.SignInAsAdminAsync();
        var variantId = await LastUnitOf(admin, "core-gum");

        var orders = new List<(HttpClient Client, string OrderNumber)>();

        for (var i = 0; i < 5; i++)
        {
            orders.Add(await AwaitingPaymentOrder(variantId));
        }

        var confirmations = await Task.WhenAll(orders.Select(o =>
            o.Client.PostAsync($"/api/payments/{o.OrderNumber}/confirm", null)));

        Assert.Equal(1, confirmations.Count(r => r.IsSuccessStatusCode));
        Assert.Equal(0, await StockOf(admin, variantId));
    }

    private async Task<int> LastUnitOf(HttpClient admin, string slug)
    {
        var product = await admin.GetFromJsonAsync<Detail>($"/api/products/{slug}");
        var variantId = product!.Variants.First(v => v.Stock > 0).Id;

        var response = await admin.PutAsJsonAsync($"/api/admin/variants/{variantId}/stock", new { stock = 1 });
        response.EnsureSuccessStatusCode();

        return variantId;
    }

    private static async Task<int> StockOf(HttpClient admin, int variantId)
    {
        var product = await admin.GetFromJsonAsync<Detail>("/api/products/axis-runner");
        var match = product!.Variants.FirstOrDefault(v => v.Id == variantId);

        if (match is not null)
        {
            return match.Stock;
        }

        var other = await admin.GetFromJsonAsync<Detail>("/api/products/core-gum");
        return other!.Variants.First(v => v.Id == variantId).Stock;
    }

    private async Task<(HttpClient Client, string OrderNumber)> AwaitingPaymentOrder(int variantId)
    {
        var client = await factory.SignInAsCustomerAsync($"corrida-{Guid.NewGuid():N}@supershop.pt");

        var address = await client.PostAsJsonAsync("/api/me/addresses", new
        {
            fullName = "Kennedy Silva",
            line1 = "Rua das Flores 12",
            line2 = (string?)null,
            postalCode = "4050-262",
            city = "Porto",
            country = "PT",
            phone = "912345678",
            isDefault = true
        });

        var addressId = (await address.Content.ReadFromJsonAsync<Address>())!.Id;

        var added = await client.PostAsJsonAsync("/api/cart/items", new { productVariantId = variantId, quantity = 1 });
        added.EnsureSuccessStatusCode();

        var placed = await client.PostAsJsonAsync("/api/orders", new
        {
            addressId,
            paymentMethod = Multibanco,
            mbWayPhone = (string?)null,
            cardNumber = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);

        var order = await placed.Content.ReadFromJsonAsync<Order>();

        return (client, order!.OrderNumber);
    }

    private record Detail(int Id, IReadOnlyList<Variant> Variants);
    private record Variant(int Id, int Stock);
    private record Address(int Id);
    private record Order(string OrderNumber, int Status);
}
