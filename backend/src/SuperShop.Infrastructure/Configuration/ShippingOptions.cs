using SuperShop.Domain.Orders;

namespace SuperShop.Infrastructure.Configuration;

public class ShippingOptions
{
    public const string SectionName = "Shipping";

    public decimal StandardCost { get; set; } = 4.90m;
    public decimal FreeShippingThreshold { get; set; } = 50.00m;

    public ShippingRules ToRules() => new(StandardCost, FreeShippingThreshold);
}

public class PaymentOptions
{
    public const string SectionName = "Payments";

    public string MultibancoEntity { get; set; } = "21234";
    public int MbWayConfirmationSeconds { get; set; } = 8;
}
