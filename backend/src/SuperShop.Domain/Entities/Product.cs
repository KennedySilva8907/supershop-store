namespace SuperShop.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int CategoryId { get; set; }
    public int CollectionId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public Collection Collection { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];

    public bool HasStock => Variants.Any(v => v.Stock > 0);

    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
}
