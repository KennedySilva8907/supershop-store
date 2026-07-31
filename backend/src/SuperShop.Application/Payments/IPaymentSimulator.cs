using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;

namespace SuperShop.Application.Payments;

public record PaymentContext(int OrderId, decimal Amount, string? MbWayPhone, string? CardNumber);

public interface IPaymentSimulator
{
    PaymentMethod Method { get; }

    Payment Create(PaymentContext context, DateTimeOffset now);

    bool ConfirmsImmediately { get; }

    bool CanConfirm(Payment payment, DateTimeOffset now);
}

public interface IPaymentSimulatorFactory
{
    IPaymentSimulator For(PaymentMethod method);
}
