using System.Net;
using System.Net.Http.Json;

namespace SuperShop.IntegrationTests;

public class EmptyResponseTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Theory]
    [InlineData("/api/auth/forgot-password")]
    [InlineData("/api/auth/resend-confirmation")]
    public async Task These_answer_202_with_nothing_in_the_body(string path)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(path, new { email = "quem-quer-que-seja@supershop.pt" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Deleting_an_address_answers_204_with_nothing_in_the_body()
    {
        var client = await factory.SignInAsCustomerAsync($"apagar-{Guid.NewGuid():N}@supershop.pt");

        var created = await client.PostAsJsonAsync("/api/me/addresses", new
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

        var id = (await created.Content.ReadFromJsonAsync<Created>())!.Id;

        var response = await client.DeleteAsync($"/api/me/addresses/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }

    private record Created(int Id);
}
