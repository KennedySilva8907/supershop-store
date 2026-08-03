using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Api.Auth;
using SuperShop.Application.Account;
using SuperShop.Application.Auth;

namespace SuperShop.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = "RequireCustomer")]
public class AccountController(
    AddressService addresses,
    AuthService auth,
    IConfiguration configuration) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPut]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await auth.UpdateProfileAsync(UserId, request, cancellationToken));

    [HttpPut("password")]
    public async Task<ActionResult<AuthResponse>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var (user, tokens) = await auth.ChangePasswordAsync(UserId, request, cancellationToken);
        RefreshCookie.Set(HttpContext, configuration, tokens.RefreshToken);

        return Ok(new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt, user));
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> List(CancellationToken cancellationToken) =>
        Ok(await addresses.ListAsync(UserId, cancellationToken));

    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> Create(
        SaveAddressRequest request,
        CancellationToken cancellationToken)
    {
        var created = await addresses.CreateAsync(UserId, request, cancellationToken);

        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpPut("addresses/{id:int}")]
    public async Task<ActionResult<AddressDto>> Update(
        int id,
        SaveAddressRequest request,
        CancellationToken cancellationToken) =>
        Ok(await addresses.UpdateAsync(UserId, id, request, cancellationToken));

    [HttpDelete("addresses/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await addresses.DeleteAsync(UserId, id, cancellationToken);

        return NoContent();
    }
}
