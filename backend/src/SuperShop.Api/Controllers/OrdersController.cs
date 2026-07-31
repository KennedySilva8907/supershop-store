using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Application.Orders;

namespace SuperShop.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "RequireCustomer")]
public class OrdersController(OrderService orders) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("orders")]
    public async Task<ActionResult<OrderDto>> Place(
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await orders.PlaceAsync(UserId, request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { orderNumber = order.OrderNumber }, order);
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await orders.ListAsync(UserId, cancellationToken));

    [HttpGet("orders/{orderNumber}")]
    public async Task<ActionResult<OrderDto>> Get(string orderNumber, CancellationToken cancellationToken) =>
        Ok(await orders.GetAsync(UserId, orderNumber, cancellationToken));

    [HttpPost("orders/{orderNumber}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(string orderNumber, CancellationToken cancellationToken) =>
        Ok(await orders.CancelAsync(UserId, orderNumber, cancellationToken));

    [HttpGet("payments/{orderNumber}")]
    public async Task<ActionResult<PaymentDto>> Payment(string orderNumber, CancellationToken cancellationToken) =>
        Ok((await orders.GetAsync(UserId, orderNumber, cancellationToken)).Payment);

    [HttpPost("payments/{orderNumber}/confirm")]
    public async Task<ActionResult<OrderDto>> ConfirmPayment(
        string orderNumber,
        CancellationToken cancellationToken) =>
        Ok(await orders.ConfirmPaymentAsync(UserId, orderNumber, cancellationToken));
}
