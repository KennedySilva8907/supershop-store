using SuperShop.Domain.Enums;

namespace SuperShop.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public SizeSystem SizeSystem { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
