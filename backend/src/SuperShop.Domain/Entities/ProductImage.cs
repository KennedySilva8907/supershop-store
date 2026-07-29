namespace SuperShop.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string PublicId { get; set; } = null!;
    public string AltText { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
