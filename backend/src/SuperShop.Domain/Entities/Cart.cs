namespace SuperShop.Domain.Entities;

public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = [];
}
