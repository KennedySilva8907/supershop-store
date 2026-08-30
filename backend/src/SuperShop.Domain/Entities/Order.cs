using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;

namespace SuperShop.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.AwaitingPayment;
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public string ShippingFullName { get; set; } = null!;
    public string ShippingLine1 { get; set; } = null!;
    public string? ShippingLine2 { get; set; }
    public string ShippingPostalCode { get; set; } = null!;
    public string ShippingCity { get; set; } = null!;
    public string ShippingCountry { get; set; } = null!;
    public string ShippingPhone { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public Payment Payment { get; set; } = null!;

    public bool HoldsStock => OrderStateMachine.HoldsStock(Status);

    public void MarkPaid(DateTimeOffset now)
    {
        OrderStateMachine.EnsureCanTransition(Status, OrderStatus.Paid);

        Status = OrderStatus.Paid;
        PaidAt ??= now;
        Payment.Confirm(now);
    }

    public void MarkShipped(DateTimeOffset now)
    {
        OrderStateMachine.EnsureCanTransition(Status, OrderStatus.Shipped);

        Status = OrderStatus.Shipped;
        ShippedAt ??= now;
    }

    public void MarkDelivered()
    {
        OrderStateMachine.EnsureCanTransition(Status, OrderStatus.Delivered);

        Status = OrderStatus.Delivered;
    }

    public void Cancel()
    {
        OrderStateMachine.EnsureCanTransition(Status, OrderStatus.Cancelled);

        Status = OrderStatus.Cancelled;
        Payment.Fail();
    }

    public void MoveTo(OrderStatus target, DateTimeOffset now)
    {
        switch (target)
        {
            case OrderStatus.Paid:
                MarkPaid(now);
                break;
            case OrderStatus.Shipped:
                MarkShipped(now);
                break;
            case OrderStatus.Delivered:
                MarkDelivered();
                break;
            case OrderStatus.Cancelled:
                Cancel();
                break;
            default:
                throw new ConflictException($"Não é possível mudar uma encomenda de {Status} para {target}.");
        }
    }
}
