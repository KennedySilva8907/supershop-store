using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SuperShop.IntegrationTests;

public class AuthRateLimitTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task A_caller_supplied_address_does_not_get_its_own_budget()
    {
        await using var host = Host(permitPerMinute: 2, onFly: false);
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "203.0.113.10", "198.51.100.1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "203.0.113.11", "198.51.100.2")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, "203.0.113.12", "198.51.100.3")).StatusCode);
    }

    [Fact]
    public async Task On_fly_the_budget_follows_fly_client_ip_and_ignores_forwarded_for()
    {
        await using var host = Host(permitPerMinute: 2, onFly: true);
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "198.51.100.1", "203.0.113.10")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "198.51.100.2", "203.0.113.10")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, "198.51.100.3", "203.0.113.10")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "203.0.113.10", "203.0.113.20")).StatusCode);
    }

    [Fact]
    public async Task Refresh_is_on_the_same_limit()
    {
        await using var host = Host(permitPerMinute: 2, onFly: false);
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/api/auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task Confirm_email_is_on_the_same_limit()
    {
        await using var host = Host(permitPerMinute: 2, onFly: false);
        var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await Confirm(client)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Confirm(client)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Confirm(client)).StatusCode);
    }

    private WebApplicationFactory<Program> Host(int permitPerMinute, bool onFly) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimit:AuthPermitPerMinute", permitPerMinute.ToString());
            if (onFly)
            {
                builder.UseSetting("FLY_APP_NAME", "supershop-api");
            }
        });

    private static async Task<HttpResponseMessage> Login(HttpClient client, string forwardedFor, string flyClientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "nobody@supershop.pt",
                password = "Password123!"
            })
        };

        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        request.Headers.TryAddWithoutValidation("Fly-Client-IP", flyClientIp);

        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> Confirm(HttpClient client) =>
        client.PostAsJsonAsync("/api/auth/confirm-email", new { userId = "missing-user", token = "not-a-token" });
}
