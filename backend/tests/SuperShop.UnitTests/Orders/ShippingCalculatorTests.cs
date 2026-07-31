using SuperShop.Domain.Orders;

namespace SuperShop.UnitTests.Orders;

public class ShippingCalculatorTests
{
    private static readonly ShippingRules Rules = ShippingRules.Default;

    [Theory]
    [InlineData(49.98, 4.90)]
    [InlineData(49.99, 4.90)]
    [InlineData(50.00, 0.00)]
    [InlineData(50.01, 0.00)]
    [InlineData(149.00, 0.00)]
    [InlineData(19.00, 4.90)]
    public void Shipping_is_free_from_fifty_euros(decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, ShippingCalculator.Calculate(subtotal, Rules).ShippingCost);
    }

    [Fact]
    public void An_empty_cart_is_not_charged_shipping()
    {
        var totals = ShippingCalculator.Calculate(0m, Rules);

        Assert.Equal(0m, totals.ShippingCost);
        Assert.Equal(0m, totals.Total);
    }

    [Theory]
    [InlineData(49.99, 4.90, 54.89)]
    [InlineData(50.00, 0.00, 50.00)]
    [InlineData(25.00, 4.90, 29.90)]
    public void Total_is_subtotal_plus_shipping(decimal subtotal, decimal shipping, decimal total)
    {
        var result = ShippingCalculator.Calculate(subtotal, Rules);

        Assert.Equal(shipping, result.ShippingCost);
        Assert.Equal(total, result.Total);
    }

    [Theory]
    [InlineData(0.00, 50.00)]
    [InlineData(19.00, 31.00)]
    [InlineData(49.99, 0.01)]
    [InlineData(50.00, 0.00)]
    [InlineData(75.00, 0.00)]
    public void Remaining_for_free_shipping_is_reported(decimal subtotal, decimal remaining)
    {
        Assert.Equal(remaining, ShippingCalculator.Calculate(subtotal, Rules).FreeShippingRemaining);
    }

    [Fact]
    public void Subtotal_is_rounded_to_two_decimals_away_from_zero()
    {
        Assert.Equal(10.13m, ShippingCalculator.Calculate(10.125m, Rules).Subtotal);
    }

    [Fact]
    public void Rules_come_from_configuration_rather_than_the_code()
    {
        var custom = new ShippingRules(2.50m, 30.00m);

        Assert.Equal(2.50m, ShippingCalculator.Calculate(29.99m, custom).ShippingCost);
        Assert.Equal(0m, ShippingCalculator.Calculate(30.00m, custom).ShippingCost);
    }
}
