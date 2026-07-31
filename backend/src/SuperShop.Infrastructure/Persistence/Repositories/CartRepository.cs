using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuperShop.Application.Cart;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;
using SuperShop.Infrastructure.Configuration;

namespace SuperShop.Infrastructure.Persistence.Repositories;

public class CartRepository(
    SuperShopDbContext context,
    IOptions<ShippingOptions> shipping,
    TimeProvider clock) : ICartRepository
{
    private ShippingRules Rules => shipping.Value.ToRules();

    public async Task<CartDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var lines = await context.CartItems
            .AsNoTracking()
            .Where(i => i.Cart.UserId == userId)
            .OrderBy(i => i.Id)
            .Select(i => new CartLineDto(
                i.Id,
                i.ProductVariantId,
                i.ProductVariant.ProductId,
                i.ProductVariant.Product.Name,
                i.ProductVariant.Product.Slug,
                i.ProductVariant.Product.Collection.Name,
                i.ProductVariant.Size.Label,
                i.ProductVariant.Sku,
                i.ProductVariant.Product.Price,
                i.Quantity,
                i.ProductVariant.Stock,
                i.ProductVariant.Product.Images
                    .OrderByDescending(m => m.IsPrimary)
                    .ThenBy(m => m.SortOrder)
                    .Select(m => m.PublicId)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var totals = ShippingCalculator.Calculate(lines.Sum(l => l.LineTotal), Rules);

        return new CartDto(lines, totals.Subtotal, totals.ShippingCost, totals.Total, totals.FreeShippingRemaining);
    }

    public async Task AddAsync(string userId, int productVariantId, int quantity, CancellationToken cancellationToken)
    {
        var variant = await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == productVariantId, cancellationToken)
            ?? throw NotFoundException.For("Variante", productVariantId);

        if (!variant.Product.IsActive)
        {
            throw new ConflictException("Este produto já não está disponível.");
        }

        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        var existing = await context.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductVariantId == productVariantId, cancellationToken);

        var wanted = (existing?.Quantity ?? 0) + quantity;

        if (wanted > variant.Stock)
        {
            throw new InsufficientStockException(variant.Sku, wanted, variant.Stock);
        }

        if (existing is null)
        {
            context.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = productVariantId,
                Quantity = quantity
            });
        }
        else
        {
            existing.Quantity = wanted;
        }

        cart.UpdatedAt = clock.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateQuantityAsync(string userId, int itemId, int quantity, CancellationToken cancellationToken)
    {
        var item = await OwnedItem(userId, itemId, cancellationToken);
        var stock = await context.ProductVariants
            .Where(v => v.Id == item.ProductVariantId)
            .Select(v => new { v.Stock, v.Sku })
            .FirstAsync(cancellationToken);

        if (quantity > stock.Stock)
        {
            throw new InsufficientStockException(stock.Sku, quantity, stock.Stock);
        }

        item.Quantity = quantity;
        item.Cart.UpdatedAt = clock.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string userId, int itemId, CancellationToken cancellationToken)
    {
        var item = await OwnedItem(userId, itemId, cancellationToken);

        context.CartItems.Remove(item);
        item.Cart.UpdatedAt = clock.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(string userId, CancellationToken cancellationToken)
    {
        var items = await context.CartItems.Where(i => i.Cart.UserId == userId).ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        context.CartItems.RemoveRange(items);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart> GetOrCreateCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await context.Carts.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId, UpdatedAt = clock.GetUtcNow() };
        context.Carts.Add(cart);
        await context.SaveChangesAsync(cancellationToken);

        return cart;
    }

    private async Task<CartItem> OwnedItem(string userId, int itemId, CancellationToken cancellationToken) =>
        await context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId, cancellationToken)
        ?? throw NotFoundException.For("Linha do carrinho", itemId);
}
