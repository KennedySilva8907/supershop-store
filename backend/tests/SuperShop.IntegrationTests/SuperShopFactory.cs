using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperShop.Application.Auth;
using SuperShop.Infrastructure.Identity;
using SuperShop.Infrastructure.Persistence;
using SuperShop.Infrastructure.Persistence.Seed;
using Testcontainers.PostgreSql;

namespace SuperShop.IntegrationTests;

public class SuperShopFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("supershop_tests")
        .WithUsername("tests")
        .WithPassword("tests")
        .Build();

    public RecordedEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-tests-secret-key-with-enough-bytes-000000");
        builder.UseSetting("Jwt:Issuer", "supershop-tests");
        builder.UseSetting("Jwt:Audience", "supershop-tests");
        builder.UseSetting("Admin:Email", "admin@supershop.pt");
        builder.UseSetting("Admin:Password", "AdminPassword123!");
        builder.UseSetting("Email:ApiKey", "");
        builder.UseSetting("Cloudinary:Url", "");
        builder.UseSetting("RateLimit:AuthPermitPerMinute", "10000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        using var scope = Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<SuperShopDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    public async Task<HttpClient> SignInAsCustomerAsync(string email)
    {
        var client = CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            firstName = "Teste",
            lastName = "Integração"
        });

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            user!.EmailConfirmed = true;
            await users.UpdateAsync(user);
        }

        return await Authenticate(client, email, "Password123!");
    }

    public async Task ResetPasswordAsync(string email, string newPassword)
    {
        string token;

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);

            token = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync(user!)));
        }

        var response = await CreateClient().PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword
        });

        response.EnsureSuccessStatusCode();
    }

    public async Task<(int Id, string OrderNumber)> PlaceOrderAsync(string email)
    {
        var client = await SignInAsCustomerAsync(email);

        var address = await client.PostAsJsonAsync("/api/me/addresses", new
        {
            fullName = "Kennedy Silva",
            line1 = "Rua das Flores 12",
            line2 = (string?)null,
            postalCode = "4050-262",
            city = "Porto",
            country = "PT",
            phone = "912345678",
            isDefault = true
        });

        var addressId = (await address.Content.ReadFromJsonAsync<CreatedAddress>())!.Id;

        var product = await client.GetFromJsonAsync<ProductDetail>("/api/products/axis-runner");
        var variant = product!.Variants.First(v => v.Stock > 0);

        await client.PostAsJsonAsync("/api/cart/items", new { productVariantId = variant.Id, quantity = 1 });

        var placed = await client.PostAsJsonAsync("/api/orders", new
        {
            addressId,
            paymentMethod = 1,
            mbWayPhone = (string?)null,
            cardNumber = (string?)null
        });

        placed.EnsureSuccessStatusCode();

        var number = (await placed.Content.ReadFromJsonAsync<PlacedOrder>())!.OrderNumber;

        var admin = await SignInAsAdminAsync();
        var all = await admin.GetFromJsonAsync<List<AdminOrderRow>>("/api/admin/orders");

        return (all!.First(o => o.OrderNumber == number).Id, number);
    }

    private record CreatedAddress(int Id);

    private record ProductDetail(List<VariantRow> Variants);

    private record VariantRow(int Id, int Stock);

    private record PlacedOrder(string OrderNumber);

    private record AdminOrderRow(int Id, string OrderNumber);

    public Task<HttpClient> SignInAsAdminAsync() =>
        Authenticate(CreateClient(), "admin@supershop.pt", "AdminPassword123!");

    private static async Task<HttpClient> Authenticate(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private record LoginResponse(string AccessToken);
}

public class RecordedEmailSender : IEmailSender
{
    private readonly List<string> _sent = [];

    public IReadOnlyList<string> Sent => _sent;

    public Task SendEmailConfirmationAsync(string email, string name, string url, CancellationToken cancellationToken = default)
    {
        _sent.Add($"confirmation:{email}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string name, string url, CancellationToken cancellationToken = default)
    {
        _sent.Add($"reset:{email}");
        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationAsync(string email, string name, OrderEmailSummary order, CancellationToken cancellationToken = default)
    {
        _sent.Add($"order:{email}:{order.OrderNumber}");
        return Task.CompletedTask;
    }
}
