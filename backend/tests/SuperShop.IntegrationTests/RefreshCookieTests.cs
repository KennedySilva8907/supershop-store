using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SuperShop.IntegrationTests;

public class RefreshCookieTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task By_default_the_cookie_is_strict_because_the_frontend_shares_the_site()
    {
        var cookie = await SignInAndReadCookie(new Uri("https://localhost"));

        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_frontend_on_another_site_gets_a_cookie_that_can_reach_it()
    {
        var cookie = await SignInAndReadCookie(new Uri("https://localhost"), frontendOnAnotherSite: true);

        Assert.Contains("samesite=none", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Over_http_the_cookie_stays_strict_because_none_would_be_rejected_without_secure()
    {
        var cookie = await SignInAndReadCookie(new Uri("http://localhost"), frontendOnAnotherSite: true);

        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Behind_a_proxy_that_terminates_tls_the_forwarded_scheme_decides()
    {
        var cookie = await SignInAndReadCookie(new Uri("http://localhost"), forwardedProto: "https");

        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_cookie_is_http_only_and_scoped_to_the_auth_endpoints()
    {
        var cookie = await SignInAndReadCookie(new Uri("https://localhost"));

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> SignInAndReadCookie(
        Uri baseAddress,
        string? forwardedProto = null,
        bool frontendOnAnotherSite = false)
    {
        var application = frontendOnAnotherSite
            ? factory.WithWebHostBuilder(builder =>
                builder.UseSetting("Auth:FrontendOnAnotherSite", "true"))
            : factory;

        var client = application.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "admin@supershop.pt",
                password = "AdminPassword123!"
            })
        };

        if (forwardedProto is not null)
        {
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        }

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return Assert.Single(response.Headers.GetValues("Set-Cookie"));
    }
}
