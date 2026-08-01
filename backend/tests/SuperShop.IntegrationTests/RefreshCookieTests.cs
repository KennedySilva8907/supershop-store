using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SuperShop.IntegrationTests;

public class RefreshCookieTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task Over_https_the_cookie_can_cross_sites_so_the_frontend_can_renew_a_session()
    {
        var cookie = await SignInAndReadCookie(new Uri("https://localhost"));

        Assert.Contains("samesite=none", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Over_http_the_cookie_stays_strict_because_none_would_be_rejected_without_secure()
    {
        var cookie = await SignInAndReadCookie(new Uri("http://localhost"));

        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_cookie_is_http_only_and_scoped_to_the_auth_endpoints()
    {
        var cookie = await SignInAndReadCookie(new Uri("https://localhost"));

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> SignInAndReadCookie(Uri baseAddress)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@supershop.pt",
            password = "AdminPassword123!"
        });

        response.EnsureSuccessStatusCode();

        return Assert.Single(response.Headers.GetValues("Set-Cookie"));
    }
}
