using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace SuperShop.IntegrationTests;

public class OrderNumberConcurrencyTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    private const int Multibanco = 1;

    [Fact]
    public async Task Two_checkouts_at_the_same_moment_both_get_a_number()
    {
        var first = await ReadyToPlace();
        var second = await ReadyToPlace();

        var responses = await Task.WhenAll(Place(first), Place(second));

        Assert.All(responses, r => Assert.True(
            r.IsSuccessStatusCode,
            $"esperava sucesso, veio {(int)r.StatusCode}"));

        var numbers = new List<string>();

        foreach (var response in responses)
        {
            var order = await response.Content.ReadFromJsonAsync<Order>();
            Assert.Matches(@"^SS-\d{4}-\d{4}$", order!.OrderNumber);
            numbers.Add(order.OrderNumber);
        }

        Assert.Equal(2, numbers.Distinct().Count());
    }

    [Fact]
    public async Task Five_checkouts_at_once_all_get_different_numbers()
    {
        var carts = new List<(HttpClient Client, int AddressId)>();

        for (var i = 0; i < 5; i++)
        {
            carts.Add(await ReadyToPlace());
        }

        var responses = await Task.WhenAll(carts.Select(Place));

        Assert.All(responses, r => Assert.True(
            r.IsSuccessStatusCode,
            $"esperava sucesso, veio {(int)r.StatusCode}"));

        var numbers = new List<string>();

        foreach (var response in responses)
        {
            numbers.Add((await response.Content.ReadFromJsonAsync<Order>())!.OrderNumber);
        }

        Assert.Equal(5, numbers.Distinct().Count());
    }

    private static Task<HttpResponseMessage> Place((HttpClient Client, int AddressId) cart) =>
        cart.Client.PostAsJsonAsync("/api/orders", new
        {
            addressId = cart.AddressId,
            paymentMethod = Multibanco,
            mbWayPhone = (string?)null,
            cardNumber = (string?)null
        });

    private async Task<(HttpClient Client, int AddressId)> ReadyToPlace()
    {
        var client = await factory.SignInAsCustomerAsync($"numero-{Guid.NewGuid():N}@supershop.pt");

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

        var product = await client.GetFromJsonAsync<Detail>("/api/products/core-tee");
        var variant = product!.Variants.First(v => v.Stock > 5);

        var added = await client.PostAsJsonAsync("/api/cart/items", new
        {
            productVariantId = variant.Id,
            quantity = 1
        });

        added.EnsureSuccessStatusCode();

        return (client, addressId);
    }

    private record Detail(IReadOnlyList<Variant> Variants);
    private record Variant(int Id, int Stock);
    private record Address(int Id);
    private record Order(string OrderNumber);
}
