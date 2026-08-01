using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class AuthorisationTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task Register_confirm_and_sign_in_gives_a_working_token()
    {
        var client = await factory.SignInAsCustomerAsync($"fluxo-{Guid.NewGuid():N}@supershop.pt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(factory.Emails.Sent, entry => entry.StartsWith("confirmation:"));
    }

    [Fact]
    public async Task Signing_in_before_confirming_the_email_is_refused()
    {
        var client = factory.CreateClient();
        var email = $"porconfirmar-{Guid.NewGuid():N}@supershop.pt";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            firstName = "Por",
            lastName = "Confirmar"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_endpoint_without_the_role_returns_forbidden()
    {
        var customer = await factory.SignInAsCustomerAsync($"cliente-{Guid.NewGuid():N}@supershop.pt");

        var response = await customer.GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_endpoint_without_any_token_returns_unauthorised()
    {
        var response = await factory.CreateClient().GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_seeded_administrator_can_reach_the_backoffice()
    {
        var admin = await factory.SignInAsAdminAsync();

        var response = await admin.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
