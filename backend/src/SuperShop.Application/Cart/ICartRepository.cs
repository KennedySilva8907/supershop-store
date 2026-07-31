namespace SuperShop.Application.Cart;

public interface ICartRepository
{
    Task<CartDto> GetAsync(string userId, CancellationToken cancellationToken);

    Task AddAsync(string userId, int productVariantId, int quantity, CancellationToken cancellationToken);

    Task UpdateQuantityAsync(string userId, int itemId, int quantity, CancellationToken cancellationToken);

    Task RemoveAsync(string userId, int itemId, CancellationToken cancellationToken);

    Task ClearAsync(string userId, CancellationToken cancellationToken);
}
