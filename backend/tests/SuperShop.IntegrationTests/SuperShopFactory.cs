using System.Net.Http.Headers;
using System.Net.Http.Json;
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
