using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SuperShop.Infrastructure.Persistence;

namespace SuperShop.IntegrationTests;

public class ExecutionStrategyTests(SuperShopFactory factory) : IClassFixture<SuperShopFactory>
{
    [Fact]
    public void The_configured_strategy_retries_on_a_dropped_connection()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SuperShopDbContext>();

        Assert.True(context.Database.CreateExecutionStrategy().RetriesOnFailure);
    }
}
