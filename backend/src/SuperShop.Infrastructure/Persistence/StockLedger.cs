using Microsoft.EntityFrameworkCore;
using SuperShop.Domain.Exceptions;

namespace SuperShop.Infrastructure.Persistence;

public sealed record StockLine(int VariantId, int Quantity, string Sku);

public sealed class StockLedger(SuperShopDbContext context)
{
    public async Task TakeAsync(IEnumerable<StockLine> lines, CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var taken = await context.ProductVariants
                .Where(v => v.Id == line.VariantId && v.Stock >= line.Quantity)
                .ExecuteUpdateAsync(
                    v => v.SetProperty(p => p.Stock, p => p.Stock - line.Quantity),
                    cancellationToken);

            if (taken == 0)
            {
                throw new InsufficientStockException(
                    line.Sku, line.Quantity, await AvailableAsync(line.VariantId, cancellationToken));
            }
        }
    }

    public async Task ReturnAsync(IEnumerable<StockLine> lines, CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            await context.ProductVariants
                .Where(v => v.Id == line.VariantId)
                .ExecuteUpdateAsync(
                    v => v.SetProperty(p => p.Stock, p => p.Stock + line.Quantity),
                    cancellationToken);
        }
    }

    private Task<int> AvailableAsync(int variantId, CancellationToken cancellationToken) =>
        context.ProductVariants
            .Where(v => v.Id == variantId)
            .Select(v => v.Stock)
            .FirstAsync(cancellationToken);
}
