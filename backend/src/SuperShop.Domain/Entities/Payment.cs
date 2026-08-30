using SuperShop.Domain.Enums;

namespace SuperShop.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }

    public string? MbEntity { get; set; }
    public string? MbReference { get; set; }
    public string? MbWayPhone { get; set; }
    public string? CardLast4 { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public Order Order { get; set; } = null!;

    public void Confirm(DateTimeOffset now)
    {
        Status = PaymentStatus.Confirmed;
        ConfirmedAt ??= now;
    }

    public void Fail() => Status = PaymentStatus.Failed;

    public void Expire() => Status = PaymentStatus.Expired;
}
