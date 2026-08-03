using System.Net;
using System.Net.Http.Json;
using SuperShop.Domain.Enums;

namespace SuperShop.IntegrationTests;

public class AdminOrderDetailTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task An_admin_sees_the_lines_the_address_and_the_payment()
    {
        var email = $"detalhe-{Guid.NewGuid():N}@supershop.pt";
        var placed = await factory.PlaceOrderAsync(email);

        var admin = await factory.SignInAsAdminAsync();
        var order = await admin.GetFromJsonAsync<Detail>($"/api/admin/orders/{placed.Id}");

        Assert.Equal(placed.OrderNumber, order!.OrderNumber);
        Assert.NotEmpty(order.Items);
        Assert.Equal(email, order.CustomerEmail);
        Assert.False(string.IsNullOrWhiteSpace(order.ShippingCity));
        Assert.Equal(order.Total, order.Subtotal + order.ShippingCost);
        Assert.Equal(order.Total, order.Payment.Amount);
    }

    [Fact]
    public async Task The_lines_carry_what_was_copied_at_the_time_of_the_order()
    {
        var placed = await factory.PlaceOrderAsync($"copias-{Guid.NewGuid():N}@supershop.pt");

        var admin = await factory.SignInAsAdminAsync();
        var order = await admin.GetFromJsonAsync<Detail>($"/api/admin/orders/{placed.Id}");

        var line = order!.Items[0];

        Assert.False(string.IsNullOrWhiteSpace(line.ProductName));
        Assert.False(string.IsNullOrWhiteSpace(line.Sku));
        Assert.False(string.IsNullOrWhiteSpace(line.SizeLabel));
        Assert.Equal(line.LineTotal, line.UnitPrice * line.Quantity);
    }

    [Fact]
    public async Task It_says_which_states_the_order_can_move_to()
    {
        var placed = await factory.PlaceOrderAsync($"estados-{Guid.NewGuid():N}@supershop.pt");

        var admin = await factory.SignInAsAdminAsync();
        var order = await admin.GetFromJsonAsync<Detail>($"/api/admin/orders/{placed.Id}");

        Assert.Contains(OrderStatus.Paid, order!.NextStates);
        Assert.Contains(OrderStatus.Cancelled, order.NextStates);
        Assert.DoesNotContain(OrderStatus.Delivered, order.NextStates);
    }

    [Fact]
    public async Task An_order_that_does_not_exist_is_a_not_found()
    {
        var admin = await factory.SignInAsAdminAsync();

        var response = await admin.GetAsync("/api/admin/orders/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_customer_cannot_read_it()
    {
        var placed = await factory.PlaceOrderAsync($"bisbilhoteiro-{Guid.NewGuid():N}@supershop.pt");
        var customer = await factory.SignInAsCustomerAsync($"outro-{Guid.NewGuid():N}@supershop.pt");

        var response = await customer.GetAsync($"/api/admin/orders/{placed.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private record Line(
        string ProductName,
        string SizeLabel,
        string Sku,
        decimal UnitPrice,
        int Quantity,
        decimal LineTotal);

    private record Pay(decimal Amount);

    private record Detail(
        int Id,
        string OrderNumber,
        string CustomerEmail,
        decimal Subtotal,
        decimal ShippingCost,
        decimal Total,
        string ShippingCity,
        List<Line> Items,
        Pay Payment,
        List<OrderStatus> NextStates);
}
