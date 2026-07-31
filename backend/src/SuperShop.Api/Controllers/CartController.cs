using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Application.Cart;

namespace SuperShop.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Policy = "RequireCustomer")]
public class CartController(CartService cart) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken cancellationToken) =>
        Ok(await cart.GetAsync(UserId, cancellationToken));

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> Add(
        AddCartItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await cart.AddAsync(UserId, request, cancellationToken));

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<CartDto>> Update(
        int id,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await cart.UpdateQuantityAsync(UserId, id, request.Quantity, cancellationToken));

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<CartDto>> Remove(int id, CancellationToken cancellationToken) =>
        Ok(await cart.RemoveAsync(UserId, id, cancellationToken));

    [HttpDelete]
    public async Task<ActionResult<CartDto>> Clear(CancellationToken cancellationToken) =>
        Ok(await cart.ClearAsync(UserId, cancellationToken));

    [HttpPost("merge")]
    public async Task<ActionResult<CartDto>> Merge(
        MergeCartRequest request,
        CancellationToken cancellationToken) =>
        Ok(await cart.MergeAsync(UserId, request, cancellationToken));
}
