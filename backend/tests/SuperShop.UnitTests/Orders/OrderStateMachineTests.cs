using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;

namespace SuperShop.UnitTests.Orders;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Paid)]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public void Valid_transitions_are_allowed(OrderStatus from, OrderStatus to)
    {
        Assert.True(OrderStateMachine.CanTransition(from, to));
        OrderStateMachine.EnsureCanTransition(from, to);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Shipped)]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Paid, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Paid, OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Paid)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.AwaitingPayment)]
    public void Invalid_transitions_are_rejected(OrderStatus from, OrderStatus to)
    {
        Assert.False(OrderStateMachine.CanTransition(from, to));
        Assert.Throws<ConflictException>(() => OrderStateMachine.EnsureCanTransition(from, to));
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void A_status_can_never_transition_to_itself(OrderStatus status)
    {
        Assert.False(OrderStateMachine.CanTransition(status, status));
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Final_states_have_nowhere_to_go(OrderStatus status)
    {
        Assert.True(OrderStateMachine.IsFinal(status));
        Assert.Empty(OrderStateMachine.NextStates(status));
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment, false)]
    [InlineData(OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void Stock_is_held_only_from_payment_onwards(OrderStatus status, bool holds)
    {
        Assert.Equal(holds, OrderStateMachine.HoldsStock(status));
    }

    [Fact]
    public void Every_status_is_covered_so_a_new_one_cannot_be_forgotten()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            Assert.NotNull(OrderStateMachine.NextStates(status));
        }
    }

    [Fact]
    public void Pending_no_longer_exists()
    {
        Assert.DoesNotContain("Pending", Enum.GetNames<OrderStatus>());
    }
}
