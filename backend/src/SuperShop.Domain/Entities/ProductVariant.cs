namespace SuperShop.Domain.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int SizeId { get; set; }
    public string Sku { get; set; } = null!;
    public int Stock { get; set; }

    public Product Product { get; set; } = null!;
    public Size Size { get; set; } = null!;

    public bool IsInStock => Stock > 0;
}
