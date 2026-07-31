using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuperShop.Application.Orders;
using SuperShop.Application.Payments;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;
using SuperShop.Infrastructure.Configuration;
using SuperShop.Infrastructure.Persistence;

namespace SuperShop.Infrastructure.Orders;

public class OrderRepository(
    SuperShopDbContext context,
    IPaymentSimulatorFactory simulators,
    IOptions<ShippingOptions> shipping,
    TimeProvider clock) : IOrderRepository
{
    public async Task<OrderDto> PlaceAsync(
        string userId,
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var items = await context.CartItems
                .Include(i => i.ProductVariant).ThenInclude(v => v.Product).ThenInclude(p => p.Collection)
                .Include(i => i.ProductVariant).ThenInclude(v => v.Size)
                .Where(i => i.Cart.UserId == userId)
                .OrderBy(i => i.Id)
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                throw new ConflictException("O carrinho está vazio.");
            }

            foreach (var item in items)
            {
                if (item.Quantity > item.ProductVariant.Stock)
                {
                    throw new InsufficientStockException(
                        item.ProductVariant.Sku, item.Quantity, item.ProductVariant.Stock);
                }
            }

            var address = await context.Addresses
                .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, cancellationToken)
                ?? throw NotFoundException.For("Morada", request.AddressId);

            var subtotal = items.Sum(i =>
                decimal.Round(i.ProductVariant.Product.Price * i.Quantity, 2, MidpointRounding.AwayFromZero));

            var totals = ShippingCalculator.Calculate(subtotal, shipping.Value.ToRules());

            var order = new Order
            {
                OrderNumber = await NextOrderNumberAsync(now, cancellationToken),
                UserId = userId,
                Status = OrderStatus.AwaitingPayment,
                Subtotal = totals.Subtotal,
                ShippingCost = totals.ShippingCost,
                Total = totals.Total,
                ShippingFullName = address.FullName,
                ShippingLine1 = address.Line1,
                ShippingLine2 = address.Line2,
                ShippingPostalCode = address.PostalCode,
                ShippingCity = address.City,
                ShippingCountry = address.Country,
                ShippingPhone = address.Phone,
                CreatedAt = now
            };

            foreach (var item in items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductVariant.Product.Name,
                    CollectionName = item.ProductVariant.Product.Collection.Name,
                    SizeLabel = item.ProductVariant.Size.Label,
                    Sku = item.ProductVariant.Sku,
                    UnitPrice = item.ProductVariant.Product.Price,
                    Quantity = item.Quantity,
                    LineTotal = decimal.Round(
                        item.ProductVariant.Product.Price * item.Quantity, 2, MidpointRounding.AwayFromZero)
                });
            }

            context.Orders.Add(order);
            await context.SaveChangesAsync(cancellationToken);

            var simulator = simulators.For(request.PaymentMethod);
            var payment = simulator.Create(
                new PaymentContext(order.Id, order.Total, request.MbWayPhone, request.CardNumber), now);

            payment.OrderId = order.Id;
            context.Payments.Add(payment);

            if (simulator.ConfirmsImmediately)
            {
                order.Status = OrderStatus.Paid;
                order.PaidAt = now;

                foreach (var item in items)
                {
                    item.ProductVariant.Stock -= item.Quantity;
                }
            }

            context.CartItems.RemoveRange(items);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await LoadAsync(userId, order.OrderNumber, cancellationToken);
        });
    }

    public async Task<OrderDto> ConfirmPaymentAsync(
        string userId,
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var order = await Tracked(userId, orderNumber, cancellationToken);

            if (order.Status == OrderStatus.Paid)
            {
                return await LoadAsync(userId, orderNumber, cancellationToken);
            }

            OrderStateMachine.EnsureCanTransition(order.Status, OrderStatus.Paid);

            var simulator = simulators.For(order.Payment.Method);

            if (!simulator.CanConfirm(order.Payment, now))
            {
                order.Payment.Status = PaymentStatus.Expired;
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                throw new ConflictException("O prazo de pagamento expirou.");
            }

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

            order.Status = OrderStatus.Paid;
            order.PaidAt = now;
            order.Payment.Status = PaymentStatus.Confirmed;
            order.Payment.ConfirmedAt = now;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await LoadAsync(userId, orderNumber, cancellationToken);
        });
    }

    public async Task<OrderDto> CancelAsync(string userId, string orderNumber, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var order = await Tracked(userId, orderNumber, cancellationToken);

            OrderStateMachine.EnsureCanTransition(order.Status, OrderStatus.Cancelled);

            if (OrderStateMachine.HoldsStock(order.Status))
            {
                foreach (var item in order.Items)
                {
                    var variant = await context.ProductVariants
                        .FirstAsync(v => v.Id == item.ProductVariantId, cancellationToken);

                    variant.Stock += item.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.Payment.Status = PaymentStatus.Failed;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await LoadAsync(userId, orderNumber, cancellationToken);
        });
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> ListAsync(string userId, CancellationToken cancellationToken) =>
        await context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.OrderNumber,
                o.Status,
                o.Total,
                o.Items.Sum(i => i.Quantity),
                o.CreatedAt,
                o.Items
                    .Select(i => i.ProductVariant.Product.Images
                        .OrderByDescending(m => m.IsPrimary)
                        .Select(m => m.PublicId)
                        .FirstOrDefault())
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

    public Task<OrderDto> GetAsync(string userId, string orderNumber, CancellationToken cancellationToken) =>
        LoadAsync(userId, orderNumber, cancellationToken);

    private async Task<OrderDto> LoadAsync(string userId, string orderNumber, CancellationToken cancellationToken) =>
        await context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.OrderNumber == orderNumber)
            .Select(o => new OrderDto(
                o.OrderNumber, o.Status, o.Subtotal, o.ShippingCost, o.Total,
                o.ShippingFullName, o.ShippingLine1, o.ShippingLine2, o.ShippingPostalCode,
                o.ShippingCity, o.ShippingCountry, o.ShippingPhone,
                o.CreatedAt, o.PaidAt, o.ShippedAt,
                o.Items.Select(i => new OrderLineDto(
                    i.ProductName, i.CollectionName, i.SizeLabel, i.Sku, i.UnitPrice, i.Quantity, i.LineTotal,
                    i.ProductVariant.Product.Images
                        .OrderByDescending(m => m.IsPrimary)
                        .Select(m => m.PublicId)
                        .FirstOrDefault()))
                    .ToList(),
                new PaymentDto(
                    o.Payment.Method, o.Payment.Status, o.Payment.Amount,
                    o.Payment.MbEntity, o.Payment.MbReference, o.Payment.MbWayPhone, o.Payment.CardLast4,
                    o.Payment.ExpiresAt, o.Payment.ConfirmedAt)))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw NotFoundException.For("Encomenda", orderNumber);

    private async Task<Order> Tracked(string userId, string orderNumber, CancellationToken cancellationToken) =>
        await context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, cancellationToken)
        ?? throw NotFoundException.For("Encomenda", orderNumber);

    private async Task<string> NextOrderNumberAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var prefix = $"SS-{now.Year}-";

        var last = await context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;

        return prefix + next.ToString("D4");
    }
}
