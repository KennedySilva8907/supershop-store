using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using SuperShop.Application.Auth;
using SuperShop.Domain.Exceptions;
using SuperShop.Infrastructure.Identity;

namespace SuperShop.Infrastructure.Auth;

public class IdentityGateway(
    UserManager<ApplicationUser> userManager,
    TokenService tokens,
    IEmailSender emailSender,
    IConfiguration configuration,
    TimeProvider clock) : IIdentityGateway
{
    private string FrontendUrl => configuration["Frontend:Url"] ?? "http://localhost:5173";

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            throw new ConflictException("Já existe uma conta com este email.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = clock.GetUtcNow()
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Roles.Customer);
        await SendConfirmationAsync(user, cancellationToken);

        return await ToDtoAsync(user);
    }

    public async Task<(UserDto User, AuthTokens Tokens)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Email ou password incorretos.");
        }

        if (!user.EmailConfirmed)
        {
            throw new ForbiddenException("Confirma o teu email antes de entrar.");
        }

        return (await ToDtoAsync(user), await tokens.IssueAsync(user, cancellationToken));
    }

    public async Task<(UserDto User, AuthTokens Tokens)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var issued = await tokens.RotateAsync(refreshToken, cancellationToken);
        var user = await userManager.FindByEmailAsync(ReadEmail(issued.AccessToken))
            ?? throw new UnauthorizedException("Sessão inválida.");

        return (await ToDtoAsync(user), issued);
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken) =>
        tokens.RevokeAsync(refreshToken, cancellationToken);

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw NotFoundException.For("Conta", request.UserId);

        if (user.EmailConfirmed)
        {
            return;
        }

        var result = await userManager.ConfirmEmailAsync(user, Decode(request.Token));

        if (!result.Succeeded)
        {
            throw new ConflictException("O link de confirmação é inválido ou expirou.");
        }
    }

    public async Task ResendConfirmationAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is not null && !user.EmailConfirmed)
        {
            await SendConfirmationAsync(user, cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return;
        }

        var token = Encode(await userManager.GeneratePasswordResetTokenAsync(user));
        var url = $"{FrontendUrl}/nova-password?email={Uri.EscapeDataString(email)}&token={token}";

        await emailSender.SendPasswordResetAsync(user.Email!, user.FirstName, url, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new ConflictException("O link de recuperação é inválido ou expirou.");

        var result = await userManager.ResetPasswordAsync(user, Decode(request.Token), request.NewPassword);

        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await tokens.RevokeAllForUserAsync(user.Id, cancellationToken);
    }

    public async Task<(UserDto User, AuthTokens Tokens)> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw NotFoundException.For("Conta", userId);

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await tokens.RevokeAllForUserAsync(user.Id, cancellationToken);

        return (await ToDtoAsync(user), await tokens.IssueAsync(user, cancellationToken));
    }

    public async Task<UserDto> GetProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw NotFoundException.For("Conta", userId);

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw NotFoundException.For("Conta", userId);

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ConflictException("O nome e o apelido são obrigatórios.");
        }

        if (request.FirstName.Length > 60 || request.LastName.Length > 60)
        {
            throw new ConflictException("O nome e o apelido têm no máximo 60 caracteres.");
        }

        if (request.PhoneNumber is { Length: > 20 })
        {
            throw new ConflictException("O telemóvel tem no máximo 20 caracteres.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return await ToDtoAsync(user);
    }

    private async Task SendConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = Encode(await userManager.GenerateEmailConfirmationTokenAsync(user));
        var url = $"{FrontendUrl}/confirmar-email?userId={user.Id}&token={token}";

        await emailSender.SendEmailConfirmationAsync(user.Email!, user.FirstName, url, cancellationToken);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user) => new(
        user.Id,
        user.Email!,
        user.FirstName,
        user.LastName,
        user.PhoneNumber,
        user.EmailConfirmed,
        [.. await userManager.GetRolesAsync(user)]);

    private static string Encode(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string Decode(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            throw new ConflictException("O link é inválido ou expirou.");
        }
    }

    private static string ReadEmail(string accessToken)
    {
        var payload = accessToken.Split('.')[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));

        return System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("email").GetString()!;
    }
}
