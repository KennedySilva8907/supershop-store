namespace SuperShop.Domain.Enums;

public enum OrderStatus
{
    AwaitingPayment = 1,
    Paid = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
