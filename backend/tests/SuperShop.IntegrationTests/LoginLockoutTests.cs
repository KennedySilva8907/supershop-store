using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SuperShop.Infrastructure.Identity;

namespace SuperShop.IntegrationTests;

public class LoginLockoutTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task Repeated_wrong_passwords_lock_that_account_and_the_message_stays_the_same()
    {
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Lockout:MaxFailedAccessAttempts", "3");
            builder.UseSetting("Lockout:DefaultLockoutSeconds", "2");
        });

        var email = $"lock-{Guid.NewGuid():N}@supershop.pt";
        var other = $"other-{Guid.NewGuid():N}@supershop.pt";
        var client = host.CreateClient();

        await RegisterAndConfirm(host, email);
        await RegisterAndConfirm(host, other);

        var unknown = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@supershop.pt",
            password = "Password123!"
        });
        var unknownDetail = await Detail(unknown);

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal("Email ou password incorretos.", unknownDetail);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1!" });

            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            Assert.Equal(unknownDetail, await Detail(wrong));
        }

        var locked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(unknownDetail, await Detail(locked));

        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            var someoneElse = await users.FindByEmailAsync(other);

            Assert.True(await users.IsLockedOutAsync(user!));
            Assert.False(await users.IsLockedOutAsync(someoneElse!));

            var remaining = user!.LockoutEnd!.Value - DateTimeOffset.UtcNow;
            Assert.InRange(remaining, TimeSpan.Zero, TimeSpan.FromSeconds(2.5));
        }

        var otherLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = other, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, otherLogin.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(3));

        var after = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    private static async Task RegisterAndConfirm(WebApplicationFactory<Program> host, string email)
    {
        var response = await host.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            firstName = "Lock",
            lastName = "Out"
        });

        response.EnsureSuccessStatusCode();

        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user!.EmailConfirmed = true;
        var updated = await users.UpdateAsync(user);

        Assert.True(updated.Succeeded);
    }

    private static async Task<string> Detail(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name.Equals("detail", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString() ?? "";
            }
        }

        throw new InvalidOperationException(body);
    }
}
