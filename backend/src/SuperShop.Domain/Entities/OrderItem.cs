namespace SuperShop.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = null!;
    public string CollectionName { get; set; } = null!;
    public string SizeLabel { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}
