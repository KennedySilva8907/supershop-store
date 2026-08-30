using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;

namespace SuperShop.UnitTests.Orders;

public class OrderTransitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_order_is_waiting_to_be_paid()
    {
        var order = NewOrder();

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Null(order.PaidAt);
        Assert.False(order.HoldsStock);
    }

    [Fact]
    public void Paying_stamps_the_date_and_confirms_the_payment()
    {
        var order = NewOrder();

        order.MarkPaid(Now);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(Now, order.PaidAt);
        Assert.Equal(PaymentStatus.Confirmed, order.Payment.Status);
        Assert.Equal(Now, order.Payment.ConfirmedAt);
        Assert.True(order.HoldsStock);
    }

    [Fact]
    public void An_order_cannot_be_paid_twice()
    {
        var order = NewOrder();
        order.MarkPaid(Now);

        Assert.Throws<ConflictException>(() => order.MarkPaid(Now.AddHours(1)));
    }

    [Fact]
    public void Nobody_can_ship_an_order_that_was_never_paid()
    {
        var order = NewOrder();

        Assert.Throws<ConflictException>(() => order.MarkShipped(Now));
        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Null(order.ShippedAt);
    }

    [Fact]
    public void Delivering_needs_the_order_to_have_been_shipped()
    {
        var order = NewOrder();
        order.MarkPaid(Now);

        Assert.Throws<ConflictException>(order.MarkDelivered);

        order.MarkShipped(Now.AddDays(1));
        order.MarkDelivered();

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.True(order.HoldsStock);
    }

    [Fact]
    public void Cancelling_a_paid_order_fails_the_payment_and_lets_the_stock_go()
    {
        var order = NewOrder();
        order.MarkPaid(Now);

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Failed, order.Payment.Status);
        Assert.False(order.HoldsStock);
    }

    [Fact]
    public void A_delivered_order_is_the_end_of_the_line()
    {
        var order = NewOrder();
        order.MarkPaid(Now);
        order.MarkShipped(Now.AddDays(1));
        order.MarkDelivered();

        Assert.Throws<ConflictException>(order.Cancel);
        Assert.Throws<ConflictException>(() => order.MarkShipped(Now.AddDays(2)));
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Cancelled)]
    public void MoveTo_takes_the_same_road_as_the_named_methods(OrderStatus target)
    {
        var order = NewOrder();

        order.MoveTo(target, Now);

        Assert.Equal(target, order.Status);
    }

    [Fact]
    public void MoveTo_refuses_a_target_no_order_can_go_back_to()
    {
        var order = NewOrder();

        Assert.Throws<ConflictException>(() => order.MoveTo(OrderStatus.AwaitingPayment, Now));
    }

    [Fact]
    public void Paying_from_the_backoffice_keeps_the_date_the_payment_already_had()
    {
        var order = NewOrder();
        order.Payment.Confirm(Now);

        order.MarkPaid(Now.AddDays(3));

        Assert.Equal(Now, order.Payment.ConfirmedAt);
        Assert.Equal(Now.AddDays(3), order.PaidAt);
    }

    private static Order NewOrder() => new()
    {
        OrderNumber = "SS-2026-0001",
        UserId = "customer",
        Total = 49.00m,
        Payment = new Payment { Method = PaymentMethod.Multibanco, Amount = 49.00m }
    };
}
