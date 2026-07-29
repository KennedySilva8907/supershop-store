using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuperShop.Infrastructure.Persistence;

namespace SuperShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Set it with dotnet user-secrets in development, or as an environment variable in production.");
        }

        services.AddDbContext<SuperShopDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
