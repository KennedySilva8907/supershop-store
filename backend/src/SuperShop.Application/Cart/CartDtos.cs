namespace SuperShop.Application.Cart;

public record CartLineDto(
    int Id,
    int ProductVariantId,
    int ProductId,
    string ProductName,
    string ProductSlug,
    string CollectionName,
    string SizeLabel,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    int StockAvailable,
    string? ImagePublicId)
{
    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
    public bool ExceedsStock => Quantity > StockAvailable;
}

public record CartDto(
    IReadOnlyList<CartLineDto> Items,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    decimal FreeShippingRemaining)
{
    public int ItemCount => Items.Sum(i => i.Quantity);
    public bool IsEmpty => Items.Count == 0;
    public bool HasStockProblem => Items.Any(i => i.ExceedsStock);
}

public record AddCartItemRequest(int ProductVariantId, int Quantity);

public record UpdateCartItemRequest(int Quantity);

public record MergeCartRequest(IReadOnlyList<AddCartItemRequest> Items);
