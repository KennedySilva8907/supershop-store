using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SuperShop.Application.Auth;
using SuperShop.Application.Orders;
using SuperShop.Application.Payments;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;
using SuperShop.Infrastructure.Configuration;
using SuperShop.Infrastructure.Identity;
using SuperShop.Infrastructure.Persistence;

namespace SuperShop.Infrastructure.Orders;

public class OrderRepository(
    SuperShopDbContext context,
    StockLedger stock,
    IPaymentSimulatorFactory simulators,
    IOptions<ShippingOptions> shipping,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    ILogger<OrderRepository> logger,
    TimeProvider clock) : IOrderRepository
{
    public async Task<OrderDto> PlaceAsync(
        string userId,
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        var placedNumber = await strategy.ExecuteAsync(async () =>
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
            order.Payment = payment;
            context.Payments.Add(payment);

            if (simulator.ConfirmsImmediately)
            {
                order.MarkPaid(now);

                await stock.TakeAsync(
                    items.Select(i => new StockLine(i.ProductVariantId, i.Quantity, i.ProductVariant.Sku)),
                    cancellationToken);
            }

            context.CartItems.RemoveRange(items);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return order.OrderNumber;
        });

        var placed = await LoadAsync(userId, placedNumber, cancellationToken);

        await NotifyAsync(userId, placed, cancellationToken);

        return placed;
    }

    private async Task NotifyAsync(string userId, OrderDto order, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await userManager.FindByIdAsync(userId);

            if (customer?.Email is null)
            {
                return;
            }

            var summary = new OrderEmailSummary(
                order.OrderNumber,
                order.Subtotal,
                order.ShippingCost,
                order.Total,
                order.ShippingFullName,
                order.ShippingLine1,
                order.ShippingLine2,
                order.ShippingPostalCode,
                order.ShippingCity,
                [.. order.Items.Select(i => new OrderEmailLine(i.ProductName, i.SizeLabel, i.Quantity, i.LineTotal))],
                PaymentLabel(order.Payment.Method));

            await emailSender.SendOrderConfirmationAsync(customer.Email, customer.FirstName, summary, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "A encomenda {OrderNumber} ficou registada mas o email de confirmação falhou.",
                order.OrderNumber);
        }
    }

    private static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Multibanco => "Multibanco",
        PaymentMethod.MbWay => "MB WAY",
        PaymentMethod.Card => "Cartão",
        PaymentMethod.CashOnDelivery => "Na entrega",
        _ => method.ToString()
    };

    public async Task<OrderDto> ConfirmPaymentAsync(
        string userId,
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var order = await Tracked(userId, orderNumber, cancellationToken);

            if (order.Status == OrderStatus.Paid)
            {
                return;
            }

            var simulator = simulators.For(order.Payment.Method);

            if (!simulator.CanConfirm(order.Payment, now))
            {
                order.Payment.Expire();
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                throw new ConflictException("O prazo de pagamento expirou.");
            }

            order.MarkPaid(now);

            await stock.TakeAsync(
                order.Items.Select(i => new StockLine(i.ProductVariantId, i.Quantity, i.Sku)),
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return await LoadAsync(userId, orderNumber, cancellationToken);
    }

    public async Task<OrderDto> CancelAsync(string userId, string orderNumber, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var order = await Tracked(userId, orderNumber, cancellationToken);

            var heldStock = order.HoldsStock;

            order.Cancel();

            if (heldStock)
            {
                await stock.ReturnAsync(
                    order.Items.Select(i => new StockLine(i.ProductVariantId, i.Quantity, i.Sku)),
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return await LoadAsync(userId, orderNumber, cancellationToken);
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
        var year = now.Year;

        var next = await context.Database
            .SqlQuery<int>($"""
                INSERT INTO "OrderNumberCounters" ("Year", "Next")
                VALUES ({year}, 1)
                ON CONFLICT ("Year")
                DO UPDATE SET "Next" = "OrderNumberCounters"."Next" + 1
                RETURNING "Next" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return $"SS-{year}-{next.Single():D4}";
    }
}
