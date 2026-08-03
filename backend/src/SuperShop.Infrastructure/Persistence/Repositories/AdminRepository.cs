using Microsoft.EntityFrameworkCore;
using SuperShop.Application.Admin;
using SuperShop.Application.Orders;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;

namespace SuperShop.Infrastructure.Persistence.Repositories;

public class AdminRepository(SuperShopDbContext context, TimeProvider clock) : IAdminRepository
{
    public async Task<IReadOnlyList<AdminProductDto>> ListProductsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking();

        if (search is not null)
        {
            var term = $"%{search}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Slug, term));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new AdminProductDto(
                p.Id, p.Name, p.Slug, p.Price, p.CompareAtPrice,
                p.Category.Name, p.Collection.Name, p.IsActive, p.IsFeatured,
                p.Variants.Sum(v => v.Stock), p.Images.Count, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminProductDto> CreateProductAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(p => p.Slug == request.Slug, cancellationToken))
        {
            throw new ConflictException($"Já existe um produto com o endereço {request.Slug}.");
        }

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw NotFoundException.For("Categoria", request.CategoryId);

        var product = new Product
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            CategoryId = request.CategoryId,
            CollectionId = request.CollectionId,
            IsActive = true,
            IsFeatured = request.IsFeatured,
            CreatedAt = clock.GetUtcNow()
        };

        var sizes = await context.Sizes
            .Where(s => s.SizeSystem == category.SizeSystem)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        var prefix = SkuPrefix(category.Slug);
        var number = await context.Products.CountAsync(cancellationToken) + 1;

        foreach (var size in sizes)
        {
            product.Variants.Add(new ProductVariant
            {
                SizeId = size.Id,
                Sku = $"SS-{prefix}-{number:D3}-{size.Label}",
                Stock = 0
            });
        }

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return await LoadProductAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductDto> UpdateProductAsync(
        int id,
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Produto", id);

        if (await context.Products.AnyAsync(p => p.Slug == request.Slug && p.Id != id, cancellationToken))
        {
            throw new ConflictException($"Já existe outro produto com o endereço {request.Slug}.");
        }

        product.Name = request.Name;
        product.Slug = request.Slug;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.CategoryId = request.CategoryId;
        product.CollectionId = request.CollectionId;
        product.IsFeatured = request.IsFeatured;

        await context.SaveChangesAsync(cancellationToken);

        return await LoadProductAsync(id, cancellationToken);
    }

    public async Task<AdminProductDto> SetProductStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw NotFoundException.For("Produto", id);

        product.IsActive = isActive;
        await context.SaveChangesAsync(cancellationToken);

        return await LoadProductAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminVariantDto>> ListVariantsAsync(
        int productId,
        CancellationToken cancellationToken) =>
        await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Size.SortOrder)
            .Select(v => new AdminVariantDto(v.Id, v.Size.Label, v.Size.SortOrder, v.Sku, v.Stock))
            .ToListAsync(cancellationToken);

    public async Task<AdminVariantDto> SetStockAsync(int variantId, int stock, CancellationToken cancellationToken)
    {
        var variant = await context.ProductVariants
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken)
            ?? throw NotFoundException.For("Variante", variantId);

        variant.Stock = stock;
        await context.SaveChangesAsync(cancellationToken);

        return new AdminVariantDto(variant.Id, variant.Size.Label, variant.Size.SortOrder, variant.Sku, variant.Stock);
    }

    public async Task<IReadOnlyList<AdminImageDto>> ListImagesAsync(
        int productId,
        CancellationToken cancellationToken) =>
        await context.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new AdminImageDto(i.Id, i.PublicId, i.AltText, i.IsPrimary, i.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<AdminImageDto> AddImageAsync(
        int productId,
        string publicId,
        string altText,
        CancellationToken cancellationToken)
    {
        if (!await context.Products.AnyAsync(p => p.Id == productId, cancellationToken))
        {
            throw NotFoundException.For("Produto", productId);
        }

        var existing = await context.ProductImages.CountAsync(i => i.ProductId == productId, cancellationToken);

        var image = new ProductImage
        {
            ProductId = productId,
            PublicId = publicId,
            AltText = altText,
            IsPrimary = existing == 0,
            SortOrder = existing + 1
        };

        context.ProductImages.Add(image);
        await context.SaveChangesAsync(cancellationToken);

        return new AdminImageDto(image.Id, image.PublicId, image.AltText, image.IsPrimary, image.SortOrder);
    }

    public async Task<string> RemoveImageAsync(int productId, int imageId, CancellationToken cancellationToken)
    {
        var image = await context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken)
            ?? throw NotFoundException.For("Imagem", imageId);

        var wasPrimary = image.IsPrimary;
        var publicId = image.PublicId;

        context.ProductImages.Remove(image);
        await context.SaveChangesAsync(cancellationToken);

        if (wasPrimary)
        {
            var replacement = await context.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacement is not null)
            {
                replacement.IsPrimary = true;
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        return publicId;
    }

    public async Task<IReadOnlyList<AdminOrderDto>> ListOrdersAsync(
        OrderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = context.Orders.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOrderDto(
                o.Id, o.OrderNumber, o.Status, o.Total,
                o.Items.Sum(i => i.Quantity),
                o.ShippingFullName, o.ShippingCity,
                o.Payment.Method, o.Payment.Status, o.CreatedAt))
            .ToListAsync(cancellationToken);

        return [.. orders.Select(o => o with { NextStates = OrderStateMachine.NextStates(o.Status) })];
    }

    public async Task<AdminOrderDetailDto> GetOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new AdminOrderDetailDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                context.Users
                    .Where(u => u.Id == o.UserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? o.ShippingFullName,
                context.Users
                    .Where(u => u.Id == o.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefault() ?? "",
                o.Subtotal,
                o.ShippingCost,
                o.Total,
                o.ShippingFullName,
                o.ShippingLine1,
                o.ShippingLine2,
                o.ShippingPostalCode,
                o.ShippingCity,
                o.ShippingCountry,
                o.ShippingPhone,
                o.CreatedAt,
                o.PaidAt,
                o.ShippedAt,
                o.Items
                    .Select(i => new OrderLineDto(
                        i.ProductName,
                        i.CollectionName,
                        i.SizeLabel,
                        i.Sku,
                        i.UnitPrice,
                        i.Quantity,
                        i.LineTotal,
                        i.ProductVariant.Product.Images
                            .OrderByDescending(m => m.IsPrimary)
                            .Select(m => m.PublicId)
                            .FirstOrDefault()))
                    .ToList(),
                new PaymentDto(
                    o.Payment.Method,
                    o.Payment.Status,
                    o.Payment.Amount,
                    o.Payment.MbEntity,
                    o.Payment.MbReference,
                    o.Payment.MbWayPhone,
                    o.Payment.CardLast4,
                    o.Payment.ExpiresAt,
                    o.Payment.ConfirmedAt)))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Encomenda", orderId);

        return order with { NextStates = OrderStateMachine.NextStates(order.Status) };
    }

    public async Task<AdminOrderDto> SetOrderStatusAsync(
        int orderId,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var order = await context.Orders
                .Include(o => o.Items)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
                ?? throw NotFoundException.For("Encomenda", orderId);

            OrderStateMachine.EnsureCanTransition(order.Status, status);

            var heldBefore = OrderStateMachine.HoldsStock(order.Status);
            var heldAfter = OrderStateMachine.HoldsStock(status);

            if (!heldBefore && heldAfter)
            {
                foreach (var item in order.Items)
                {
                    var variant = await context.ProductVariants
                        .FirstAsync(v => v.Id == item.ProductVariantId, cancellationToken);

                    if (item.Quantity > variant.Stock)
                    {
                        throw new InsufficientStockException(item.Sku, item.Quantity, variant.Stock);
                    }

                    variant.Stock -= item.Quantity;
                }
            }
            else if (heldBefore && !heldAfter)
            {
                foreach (var item in order.Items)
                {
                    var variant = await context.ProductVariants
                        .FirstAsync(v => v.Id == item.ProductVariantId, cancellationToken);

                    variant.Stock += item.Quantity;
                }
            }

            order.Status = status;

            if (status == OrderStatus.Paid)
            {
                order.PaidAt ??= now;
                order.Payment.Status = PaymentStatus.Confirmed;
                order.Payment.ConfirmedAt ??= now;
            }

            if (status == OrderStatus.Shipped)
            {
                order.ShippedAt ??= now;
            }

            if (status == OrderStatus.Cancelled)
            {
                order.Payment.Status = PaymentStatus.Failed;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var updated = await context.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new AdminOrderDto(
                    o.Id, o.OrderNumber, o.Status, o.Total,
                    o.Items.Sum(i => i.Quantity),
                    o.ShippingFullName, o.ShippingCity,
                    o.Payment.Method, o.Payment.Status, o.CreatedAt))
                .FirstAsync(cancellationToken);

            return updated with { NextStates = OrderStateMachine.NextStates(updated.Status) };
        });
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var paidStatuses = new[] { OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Delivered };

        var lowStock = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Stock < AdminService.LowStockThreshold && v.Product.IsActive)
            .OrderBy(v => v.Stock)
            .ThenBy(v => v.Product.Name)
            .Take(20)
            .Select(v => new LowStockDto(v.Id, v.Product.Name, v.Size.Label, v.Sku, v.Stock))
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            await context.Orders.Where(o => paidStatuses.Contains(o.Status)).SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m,
            await context.Orders.CountAsync(o => paidStatuses.Contains(o.Status), cancellationToken),
            await context.Orders.CountAsync(o => o.Status == OrderStatus.AwaitingPayment, cancellationToken),
            await context.Products.CountAsync(cancellationToken),
            await context.Products.CountAsync(p => !p.IsActive, cancellationToken),
            await context.Products.CountAsync(p => p.IsActive && !p.Variants.Any(v => v.Stock > 0), cancellationToken),
            lowStock);
    }

    private async Task<AdminProductDto> LoadProductAsync(int id, CancellationToken cancellationToken) =>
        await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new AdminProductDto(
                p.Id, p.Name, p.Slug, p.Price, p.CompareAtPrice,
                p.Category.Name, p.Collection.Name, p.IsActive, p.IsFeatured,
                p.Variants.Sum(v => v.Stock), p.Images.Count, p.CreatedAt))
            .FirstAsync(cancellationToken);

    private static string SkuPrefix(string categorySlug) => categorySlug switch
    {
        "sapatilhas" => "SAP",
        "t-shirts" => "TSH",
        "casacos" => "CAS",
        "calcas" => "CLA",
        "calcoes" => "CLC",
        _ => "GEN"
    };
}
