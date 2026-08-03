using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class ChangePasswordTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task The_new_password_is_the_one_that_works_afterwards()
    {
        var email = $"trocar-{Guid.NewGuid():N}@supershop.pt";
        var client = await factory.SignInAsCustomerAsync(email);

        var change = await client.PutAsJsonAsync("/api/me/password", new
        {
            currentPassword = "Password123!",
            newPassword = "Password456!"
        });

        change.EnsureSuccessStatusCode();

        var fresh = factory.CreateClient();

        var withOld = await fresh.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        var withNew = await fresh.PostAsJsonAsync("/api/auth/login", new { email, password = "Password456!" });

        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
    }

    [Fact]
    public async Task The_wrong_current_password_changes_nothing()
    {
        var email = $"erradaatual-{Guid.NewGuid():N}@supershop.pt";
        var client = await factory.SignInAsCustomerAsync(email);

        var response = await client.PutAsJsonAsync("/api/me/password", new
        {
            currentPassword = "NaoEAMinha1!",
            newPassword = "Password456!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var stillWorks = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    [Fact]
    public async Task Repeating_the_current_password_is_refused()
    {
        var client = await factory.SignInAsCustomerAsync($"mesma-{Guid.NewGuid():N}@supershop.pt");

        var response = await client.PutAsJsonAsync("/api/me/password", new
        {
            currentPassword = "Password123!",
            newPassword = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Changing_the_password_throws_out_a_session_somebody_else_is_holding()
    {
        var email = $"roubada-{Guid.NewGuid():N}@supershop.pt";
        var owner = await factory.SignInAsCustomerAsync(email);

        var thief = factory.CreateClient();
        var stolen = await thief.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        stolen.EnsureSuccessStatusCode();

        var refreshedBefore = await thief.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshedBefore.StatusCode);

        var change = await owner.PutAsJsonAsync("/api/me/password", new
        {
            currentPassword = "Password123!",
            newPassword = "Password456!"
        });

        change.EnsureSuccessStatusCode();

        var refreshedAfter = await thief.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshedAfter.StatusCode);
    }

    [Fact]
    public async Task Resetting_the_password_throws_out_the_old_sessions_too()
    {
        var email = $"reposta-{Guid.NewGuid():N}@supershop.pt";
        await factory.SignInAsCustomerAsync(email);

        var thief = factory.CreateClient();
        var stolen = await thief.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        stolen.EnsureSuccessStatusCode();

        await factory.ResetPasswordAsync(email, "Password789!");

        var refreshedAfter = await thief.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshedAfter.StatusCode);
    }

    [Fact]
    public async Task Changing_a_password_without_a_token_is_refused()
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/api/me/password", new
        {
            currentPassword = "Password123!",
            newPassword = "Password456!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
