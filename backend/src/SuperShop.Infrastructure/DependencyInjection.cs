using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuperShop.Infrastructure.Identity;
using SuperShop.Infrastructure.Persistence;
using SuperShop.Application.Auth;
using SuperShop.Application.Catalog;
using SuperShop.Infrastructure.Auth;
using SuperShop.Infrastructure.Email;
using SuperShop.Infrastructure.Persistence.Repositories;
using SuperShop.Infrastructure.Persistence.Seed;

namespace SuperShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
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

        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SuperShopDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<DatabaseSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TokenService>();
        services.AddScoped<IIdentityGateway, IdentityGateway>();
        services.AddScoped<AuthService>();

        if (isDevelopment)
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }
        else
        {
            services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.brevo.com/");
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("api-key", configuration["Email:ApiKey"]);
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SuperShop/1.0");
            });
        }

        services.AddHealthChecks()
            .AddDbContextCheck<SuperShopDbContext>("database");

        return services;
    }
}
