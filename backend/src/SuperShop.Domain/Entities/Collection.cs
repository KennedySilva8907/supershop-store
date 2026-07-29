namespace SuperShop.Domain.Entities;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = [];
}
