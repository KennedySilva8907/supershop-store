using SuperShop.Domain.Enums;

namespace SuperShop.Domain.Entities;

public class Size
{
    public int Id { get; set; }
    public SizeSystem SizeSystem { get; set; }
    public string Label { get; set; } = null!;
    public int SortOrder { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = [];
}
