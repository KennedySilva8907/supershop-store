using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class ProfileTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public async Task A_customer_can_change_their_name_and_phone()
    {
        var client = await factory.SignInAsCustomerAsync($"perfil-{Guid.NewGuid():N}@supershop.pt");

        var response = await client.PutAsJsonAsync("/api/me", new
        {
            firstName = "Kennedy",
            lastName = "Silva",
            phoneNumber = "912345678"
        });

        response.EnsureSuccessStatusCode();

        var profile = await client.GetFromJsonAsync<Profile>("/api/auth/me");

        Assert.Equal("Kennedy", profile!.FirstName);
        Assert.Equal("Silva", profile.LastName);
        Assert.Equal("912345678", profile.PhoneNumber);
    }

    [Fact]
    public async Task An_empty_name_is_refused()
    {
        var client = await factory.SignInAsCustomerAsync($"semnome-{Guid.NewGuid():N}@supershop.pt");

        var response = await client.PutAsJsonAsync("/api/me", new
        {
            firstName = "   ",
            lastName = "Silva",
            phoneNumber = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_phone_can_be_cleared()
    {
        var client = await factory.SignInAsCustomerAsync($"semtelefone-{Guid.NewGuid():N}@supershop.pt");

        await client.PutAsJsonAsync("/api/me", new
        {
            firstName = "Kennedy",
            lastName = "Silva",
            phoneNumber = "912345678"
        });

        await client.PutAsJsonAsync("/api/me", new
        {
            firstName = "Kennedy",
            lastName = "Silva",
            phoneNumber = ""
        });

        var profile = await client.GetFromJsonAsync<Profile>("/api/auth/me");

        Assert.Null(profile!.PhoneNumber);
    }

    [Fact]
    public async Task Editing_a_profile_without_a_token_is_refused()
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/api/me", new
        {
            firstName = "Kennedy",
            lastName = "Silva",
            phoneNumber = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_edit_somebody_else()
    {
        var first = $"dono-{Guid.NewGuid():N}@supershop.pt";
        await factory.SignInAsCustomerAsync(first);

        var other = await factory.SignInAsCustomerAsync($"intruso-{Guid.NewGuid():N}@supershop.pt");

        await other.PutAsJsonAsync("/api/me", new
        {
            firstName = "Intruso",
            lastName = "Qualquer",
            phoneNumber = (string?)null
        });

        var owner = await factory.SignInAsCustomerAsync(first);
        var profile = await owner.GetFromJsonAsync<Profile>("/api/auth/me");

        Assert.NotEqual("Intruso", profile!.FirstName);
    }

    private record Profile(string FirstName, string LastName, string? PhoneNumber);
}
