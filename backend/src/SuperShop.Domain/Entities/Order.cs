using SuperShop.Domain.Enums;

namespace SuperShop.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public string ShippingFullName { get; set; } = null!;
    public string ShippingLine1 { get; set; } = null!;
    public string? ShippingLine2 { get; set; }
    public string ShippingPostalCode { get; set; } = null!;
    public string ShippingCity { get; set; } = null!;
    public string ShippingCountry { get; set; } = null!;
    public string ShippingPhone { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public Payment Payment { get; set; } = null!;
}
