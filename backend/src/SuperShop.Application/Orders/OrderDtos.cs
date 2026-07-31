using SuperShop.Domain.Enums;

namespace SuperShop.Application.Orders;

public record PlaceOrderRequest(int AddressId, PaymentMethod PaymentMethod, string? MbWayPhone, string? CardNumber);

public record OrderLineDto(
    string ProductName,
    string CollectionName,
    string SizeLabel,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ImagePublicId);

public record PaymentDto(
    PaymentMethod Method,
    PaymentStatus Status,
    decimal Amount,
    string? MbEntity,
    string? MbReference,
    string? MbWayPhone,
    string? CardLast4,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? ConfirmedAt);

public record OrderDto(
    string OrderNumber,
    OrderStatus Status,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string ShippingFullName,
    string ShippingLine1,
    string? ShippingLine2,
    string ShippingPostalCode,
    string ShippingCity,
    string ShippingCountry,
    string ShippingPhone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    IReadOnlyList<OrderLineDto> Items,
    PaymentDto Payment)
{
    public bool CanCancel => Status is OrderStatus.AwaitingPayment or OrderStatus.Paid;
}

public record OrderSummaryDto(
    string OrderNumber,
    OrderStatus Status,
    decimal Total,
    int ItemCount,
    DateTimeOffset CreatedAt,
    string? FirstImagePublicId);
