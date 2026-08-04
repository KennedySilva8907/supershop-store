using Microsoft.Extensions.Options;
using SuperShop.Application.Payments;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;
using SuperShop.Domain.Exceptions;
using SuperShop.Domain.Orders;
using SuperShop.Infrastructure.Configuration;

namespace SuperShop.Infrastructure.Payments;

public class MultibancoSimulator(IOptions<PaymentOptions> options) : IPaymentSimulator
{
    public PaymentMethod Method => PaymentMethod.Multibanco;
    public bool ConfirmsImmediately => false;

    public Payment Create(PaymentContext context, DateTimeOffset now)
    {
        var details = MultibancoReference.Generate(
            options.Value.MultibancoEntity, context.OrderId, context.Amount, now);

        return new Payment
        {
            Method = Method,
            Status = PaymentStatus.Pending,
            Amount = details.Amount,
            MbEntity = details.Entity,
            MbReference = details.Reference,
            ExpiresAt = details.ExpiresAt
        };
    }

    public bool CanConfirm(Payment payment, DateTimeOffset now) =>
        payment.ExpiresAt is null || payment.ExpiresAt > now;
}

public class MbWaySimulator : IPaymentSimulator
{
    public PaymentMethod Method => PaymentMethod.MbWay;
    public bool ConfirmsImmediately => false;

    public Payment Create(PaymentContext context, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(context.MbWayPhone))
        {
            throw new ConflictException("Indica o número de telemóvel para pagar por MB WAY.");
        }

        var phone = new string(context.MbWayPhone.Where(char.IsDigit).ToArray());

        if (phone.Length != 9 || phone[0] != '9')
        {
            throw new ConflictException("O número de telemóvel não é válido.");
        }

        return new Payment
        {
            Method = Method,
            Status = PaymentStatus.Pending,
            Amount = decimal.Round(context.Amount, 2, MidpointRounding.AwayFromZero),
            MbWayPhone = phone,
            ExpiresAt = now.AddMinutes(5)
        };
    }

    public bool CanConfirm(Payment payment, DateTimeOffset now) =>
        payment.ExpiresAt is null || payment.ExpiresAt > now;
}

public class CardSimulator : IPaymentSimulator
{
    public PaymentMethod Method => PaymentMethod.Card;
    public bool ConfirmsImmediately => true;

    public Payment Create(PaymentContext context, DateTimeOffset now)
    {
        if (!CardNumber.IsValid(context.CardNumber))
        {
            throw new ConflictException("O número do cartão não é válido.");
        }

        return new Payment
        {
            Method = Method,
            Status = PaymentStatus.Confirmed,
            Amount = decimal.Round(context.Amount, 2, MidpointRounding.AwayFromZero),
            CardLast4 = CardNumber.LastFour(context.CardNumber!),
            ConfirmedAt = now
        };
    }

    public bool CanConfirm(Payment payment, DateTimeOffset now) => true;
}

public class CashOnDeliverySimulator : IPaymentSimulator
{
    public PaymentMethod Method => PaymentMethod.CashOnDelivery;
    public bool ConfirmsImmediately => false;

    public Payment Create(PaymentContext context, DateTimeOffset now) => new()
    {
        Method = Method,
        Status = PaymentStatus.Pending,
        Amount = decimal.Round(context.Amount, 2, MidpointRounding.AwayFromZero)
    };

    public bool CanConfirm(Payment payment, DateTimeOffset now) => true;
}

public class PaymentSimulatorFactory(IEnumerable<IPaymentSimulator> simulators) : IPaymentSimulatorFactory
{
    private readonly Dictionary<PaymentMethod, IPaymentSimulator> _byMethod =
        simulators.ToDictionary(s => s.Method);

    public IPaymentSimulator For(PaymentMethod method) =>
        _byMethod.TryGetValue(method, out var simulator)
            ? simulator
            : throw new ConflictException($"O método de pagamento {method} não está disponível.");
}
