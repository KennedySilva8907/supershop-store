namespace SuperShop.Domain.Orders;

public record ShippingRules(decimal StandardCost, decimal FreeShippingThreshold)
{
    public static readonly ShippingRules Default = new(4.90m, 50.00m);
}

public record CartTotals(decimal Subtotal, decimal ShippingCost, decimal Total, decimal FreeShippingRemaining);

public static class ShippingCalculator
{
    public static CartTotals Calculate(decimal subtotal, ShippingRules rules)
    {
        var rounded = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);

        var shipping = rounded >= rules.FreeShippingThreshold || rounded == 0
            ? 0m
            : rules.StandardCost;

        var remaining = rounded >= rules.FreeShippingThreshold
            ? 0m
            : decimal.Round(rules.FreeShippingThreshold - rounded, 2, MidpointRounding.AwayFromZero);

        return new CartTotals(rounded, shipping, rounded + shipping, remaining);
    }
}
