using SuperShop.Domain.Exceptions;

namespace SuperShop.Application.Cart;

public class CartService(ICartRepository repository)
{
    public const int MaxQuantityPerLine = 20;

    public Task<CartDto> GetAsync(string userId, CancellationToken cancellationToken = default) =>
        repository.GetAsync(userId, cancellationToken);

    public async Task<CartDto> AddAsync(
        string userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await repository.AddAsync(userId, request.ProductVariantId, Quantity(request.Quantity), cancellationToken);

        return await repository.GetAsync(userId, cancellationToken);
    }

    public async Task<CartDto> UpdateQuantityAsync(
        string userId,
        int itemId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            await repository.RemoveAsync(userId, itemId, cancellationToken);
        }
        else
        {
            await repository.UpdateQuantityAsync(userId, itemId, Quantity(quantity), cancellationToken);
        }

        return await repository.GetAsync(userId, cancellationToken);
    }

    public async Task<CartDto> RemoveAsync(string userId, int itemId, CancellationToken cancellationToken = default)
    {
        await repository.RemoveAsync(userId, itemId, cancellationToken);

        return await repository.GetAsync(userId, cancellationToken);
    }

    public async Task<CartDto> ClearAsync(string userId, CancellationToken cancellationToken = default)
    {
        await repository.ClearAsync(userId, cancellationToken);

        return await repository.GetAsync(userId, cancellationToken);
    }

    public async Task<CartDto> MergeAsync(
        string userId,
        MergeCartRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in request.Items.Where(i => i.Quantity > 0))
        {
            try
            {
                await repository.AddAsync(userId, item.ProductVariantId, Quantity(item.Quantity), cancellationToken);
            }
            catch (NotFoundException)
            {
                continue;
            }
            catch (InsufficientStockException)
            {
                continue;
            }
        }

        return await repository.GetAsync(userId, cancellationToken);
    }

    private static int Quantity(int requested) => Math.Clamp(requested, 1, MaxQuantityPerLine);
}
