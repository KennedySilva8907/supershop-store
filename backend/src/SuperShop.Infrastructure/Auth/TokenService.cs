using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperShop.Application.Auth;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Exceptions;
using SuperShop.Infrastructure.Identity;
using SuperShop.Infrastructure.Persistence;

namespace SuperShop.Infrastructure.Auth;

public class TokenService(
    SuperShopDbContext context,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    TimeProvider clock)
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthTokens> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var (accessToken, expiresAt) = await CreateAccessTokenAsync(user, now);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, now, replacing: null, cancellationToken);

        return new AuthTokens(accessToken, expiresAt, refreshToken);
    }

    public async Task<AuthTokens> RotateAsync(string presentedToken, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = Hash(presentedToken);

        var stored = await context.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedException("Sessão inválida.");

        if (!stored.IsActive(now))
        {
            throw new UnauthorizedException("Sessão expirada.");
        }

        var user = await userManager.FindByIdAsync(stored.UserId)
            ?? throw new UnauthorizedException("Sessão inválida.");

        stored.RevokedAt = now;

        var (accessToken, expiresAt) = await CreateAccessTokenAsync(user, now);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, now, stored, cancellationToken);

        return new AuthTokens(accessToken, expiresAt, refreshToken);
    }

    public async Task RevokeAsync(string presentedToken, CancellationToken cancellationToken)
    {
        var hash = Hash(presentedToken);

        var stored = await context.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null)
        {
            return;
        }

        stored.RevokedAt = clock.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<(string Token, DateTimeOffset ExpiresAt)> CreateAccessTokenAsync(
        ApplicationUser user,
        DateTimeOffset now)
    {
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email_confirmed", user.EmailConfirmed ? "true" : "false")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<string> CreateRefreshTokenAsync(
        string userId,
        DateTimeOffset now,
        RefreshToken? replacing,
        CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(raw),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays)
        };

        context.Set<RefreshToken>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        if (replacing is not null)
        {
            replacing.ReplacedByTokenId = entity.Id;
            await context.SaveChangesAsync(cancellationToken);
        }

        return raw;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
