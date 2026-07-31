using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;

namespace SuperShop.Domain.Orders;

public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.AwaitingPayment] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IReadOnlyList<OrderStatus> NextStates(OrderStatus from) =>
        Allowed.TryGetValue(from, out var next) ? next : [];

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        NextStates(from).Contains(to);

    public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new ConflictException($"Não é possível mudar uma encomenda de {from} para {to}.");
        }
    }

    public static bool IsFinal(OrderStatus status) => NextStates(status).Count == 0;

    public static bool HoldsStock(OrderStatus status) =>
        status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered;
}
