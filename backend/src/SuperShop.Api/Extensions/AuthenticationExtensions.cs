using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SuperShop.Application.Auth;
using SuperShop.Infrastructure.Identity;

namespace SuperShop.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (Encoding.UTF8.GetByteCount(jwt.Secret) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 bytes. Set it with dotnet user-secrets in development, " +
                "or as an environment variable in production.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireCustomer", policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("email_confirmed", "true"));

            options.AddPolicy("RequireAdmin", policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(Roles.Admin));
        });

        return services;
    }

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitPerMinute = configuration.GetValue<int?>("RateLimit:AuthPermitPerMinute") ?? 5;
        var onFly = !string.IsNullOrWhiteSpace(configuration["FLY_APP_NAME"]);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                ClientAddress(context, onFly),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
    }

    private static string ClientAddress(HttpContext context, bool onFly)
    {
        if (onFly && FlyClientIp(context) is { } address)
        {
            return address;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string? FlyClientIp(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Fly-Client-IP", out var value))
        {
            return null;
        }

        var text = value.ToString().Trim();

        return IPAddress.TryParse(text, out var parsed) ? parsed.ToString() : null;
    }
}
