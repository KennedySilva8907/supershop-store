using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuperShop.Infrastructure.Identity;
using SuperShop.Infrastructure.Persistence;
using SuperShop.Application.Account;
using SuperShop.Application.Admin;
using SuperShop.Application.Cart;
using SuperShop.Application.Orders;
using SuperShop.Application.Payments;
using SuperShop.Infrastructure.Orders;
using SuperShop.Infrastructure.Payments;
using SuperShop.Infrastructure.Storage;
using SuperShop.Infrastructure.Configuration;
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
        bool isProduction)
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
            .AddErrorDescriber<PortugueseIdentityErrors>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<DatabaseSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<ShippingOptions>(configuration.GetSection(ShippingOptions.SectionName));
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TokenService>();
        services.AddScoped<IIdentityGateway, IdentityGateway>();
        services.AddScoped<AuthService>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<AddressService>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<CartService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<OrderService>();
        services.AddScoped<IPaymentSimulator, MultibancoSimulator>();
        services.AddScoped<IPaymentSimulator, MbWaySimulator>();
        services.AddScoped<IPaymentSimulator, CardSimulator>();
        services.AddScoped<IPaymentSimulator, CashOnDeliverySimulator>();
        services.AddScoped<IPaymentSimulatorFactory, PaymentSimulatorFactory>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<AdminService>();

        var cloudinaryUrl = configuration["Cloudinary:Url"];

        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            services.AddScoped<IImageStorage, UnavailableImageStorage>();
        }
        else
        {
            services.AddSingleton(new CloudinaryDotNet.Cloudinary(cloudinaryUrl) { Api = { Secure = true } });
            services.AddScoped<IImageStorage, CloudinaryImageStorage>();
        }

        var emailApiKey = configuration["Email:ApiKey"];

        if (string.IsNullOrWhiteSpace(emailApiKey))
        {
            if (isProduction)
            {
                throw new InvalidOperationException(
                    "Email:ApiKey is not configured. Set it as an environment variable in production.");
            }

            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }
        else
        {
            services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.brevo.com/");
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("api-key", emailApiKey);
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SuperShop/1.0");
            });
        }

        services.AddHealthChecks()
            .AddDbContextCheck<SuperShopDbContext>("database");

        return services;
    }
}
