using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuperShop.Application.Auth;

namespace SuperShop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    private const string RefreshCookie = "supershop_refresh";

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserDto>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        Ok(await auth.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var (user, tokens) = await auth.LoginAsync(request, cancellationToken);
        SetRefreshCookie(tokens.RefreshToken);

        return Ok(new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt, user));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var (user, tokens) = await auth.RefreshAsync(Request.Cookies[RefreshCookie], cancellationToken);
        SetRefreshCookie(tokens.RefreshToken);

        return Ok(new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt, user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(Request.Cookies[RefreshCookie], cancellationToken);
        Response.Cookies.Delete(RefreshCookie, BuildCookieOptions(Request.IsHttps));

        return NoContent();
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ConfirmEmailAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ResendConfirmationAsync(request.Email, cancellationToken);

        return Accepted();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ForgotPasswordAsync(request.Email, cancellationToken);

        return Accepted();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await auth.ResetPasswordAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await auth.GetProfileAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken));

    private void SetRefreshCookie(string token) =>
        Response.Cookies.Append(RefreshCookie, token, BuildCookieOptions(Request.IsHttps));

    private static CookieOptions BuildCookieOptions(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    };
}
