using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class CheckoutTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    private const int CartLineLimit = 20;

    [Fact]
    public async Task Cart_to_checkout_creates_the_order_and_debits_stock()
    {
        var client = await factory.SignInAsCustomerAsync($"compra-{Guid.NewGuid():N}@supershop.pt");
        var address = await CreateAddress(client);
        var (variantId, stockBefore, _) = await PickVariant(client, minimumStock: 2);

        await client.PostAsJsonAsync("/api/cart/items", new { productVariantId = variantId, quantity = 2 });

        var placed = await client.PostAsJsonAsync("/api/orders", new
        {
            addressId = address,
            paymentMethod = 3,
            mbWayPhone = (string?)null,
            cardNumber = "4539578763621486"
        });

        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);

        var order = await placed.Content.ReadFromJsonAsync<Order>();
        Assert.Matches(@"^SS-\d{4}-\d{4}$", order!.OrderNumber);
        Assert.Equal(2, order.Status);
        Assert.Equal("1486", order.Payment.CardLast4);

        var (_, stockAfter, _) = await PickVariant(client, minimumStock: 0, variantId);
        Assert.Equal(stockBefore - 2, stockAfter);

        var cart = await client.GetFromJsonAsync<Cart>("/api/cart");
        Assert.Empty(cart!.Items);

        Assert.Contains(factory.Emails.Sent, entry => entry.StartsWith($"order:") && entry.EndsWith(order.OrderNumber));
    }

    [Fact]
    public async Task Asking_for_more_than_the_available_stock_is_refused()
    {
        var client = await factory.SignInAsCustomerAsync($"semstock-{Guid.NewGuid():N}@supershop.pt");

        var product = await client.GetFromJsonAsync<Detail>("/api/products/axis-runner");

        var variant = product!.Variants.First(v => v.Stock > 0 && v.Stock < CartLineLimit);

        var response = await client.PostAsJsonAsync("/api/cart/items", new
        {
            productVariantId = variant.Id,
            quantity = variant.Stock + 1
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_quantity_above_the_line_limit_is_clamped_rather_than_rejected()
    {
        var client = await factory.SignInAsCustomerAsync($"limite-{Guid.NewGuid():N}@supershop.pt");

        var product = await client.GetFromJsonAsync<Detail>("/api/products/axis-runner");
        var variant = product!.Variants.First(v => v.Stock > CartLineLimit);

        var response = await client.PostAsJsonAsync("/api/cart/items", new
        {
            productVariantId = variant.Id,
            quantity = 9999
        });

        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartWithLines>();
        Assert.Equal(CartLineLimit, cart!.Items.Single().Quantity);
    }

    [Fact]
    public async Task Cancelling_a_paid_order_returns_the_stock()
    {
        var client = await factory.SignInAsCustomerAsync($"cancela-{Guid.NewGuid():N}@supershop.pt");
        var address = await CreateAddress(client);
        var (variantId, stockBefore, _) = await PickVariant(client, minimumStock: 1);

        await client.PostAsJsonAsync("/api/cart/items", new { productVariantId = variantId, quantity = 1 });

        var placed = await client.PostAsJsonAsync("/api/orders", new
        {
            addressId = address,
            paymentMethod = 3,
            mbWayPhone = (string?)null,
            cardNumber = "4539578763621486"
        });

        var order = await placed.Content.ReadFromJsonAsync<Order>();

        var cancelled = await client.PostAsync($"/api/orders/{order!.OrderNumber}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);

        var (_, stockAfter, _) = await PickVariant(client, minimumStock: 0, variantId);
        Assert.Equal(stockBefore, stockAfter);

        var again = await client.PostAsync($"/api/orders/{order.OrderNumber}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task One_customer_cannot_read_another_customers_order()
    {
        var buyer = await factory.SignInAsCustomerAsync($"dono-{Guid.NewGuid():N}@supershop.pt");
        var address = await CreateAddress(buyer);
        var (variantId, _, _) = await PickVariant(buyer, minimumStock: 1);

        await buyer.PostAsJsonAsync("/api/cart/items", new { productVariantId = variantId, quantity = 1 });

        var placed = await buyer.PostAsJsonAsync("/api/orders", new
        {
            addressId = address,
            paymentMethod = 4,
            mbWayPhone = (string?)null,
            cardNumber = (string?)null
        });

        var order = await placed.Content.ReadFromJsonAsync<Order>();

        var stranger = await factory.SignInAsCustomerAsync($"intruso-{Guid.NewGuid():N}@supershop.pt");
        var response = await stranger.GetAsync($"/api/orders/{order!.OrderNumber}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<int> CreateAddress(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/me/addresses", new
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

        return (await response.Content.ReadFromJsonAsync<Address>())!.Id;
    }

    private static async Task<(int Id, int Stock, string Sku)> PickVariant(
        HttpClient client,
        int minimumStock,
        int? preferId = null)
    {
        var product = await client.GetFromJsonAsync<Detail>("/api/products/axis-runner");

        var variant = preferId is null
            ? product!.Variants.First(v => v.Stock >= minimumStock && v.Stock > 0)
            : product!.Variants.First(v => v.Id == preferId);

        return (variant.Id, variant.Stock, variant.Sku);
    }

    private record Detail(List<Variant> Variants);

    private record Variant(int Id, int Stock, string Sku);

    private record Address(int Id);

    private record Order(string OrderNumber, int Status, PaymentInfo Payment);

    private record PaymentInfo(string? CardLast4);

    private record Cart(List<object> Items);

    private record CartWithLines(List<CartLine> Items);

    private record CartLine(int Quantity);
}
