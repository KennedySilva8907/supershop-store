using SuperShop.Application.Orders;
using SuperShop.Domain.Enums;

namespace SuperShop.Application.Admin;

public record AdminProductDto(
    int Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    string CategoryName,
    string CollectionName,
    bool IsActive,
    bool IsFeatured,
    int TotalStock,
    int ImageCount,
    DateTimeOffset CreatedAt);

public record SaveProductRequest(
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? CompareAtPrice,
    int CategoryId,
    int CollectionId,
    bool IsFeatured);

public record SetProductStatusRequest(bool IsActive);

public record AdminProductFormDto(
    int Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? CompareAtPrice,
    int CategoryId,
    int CollectionId,
    bool IsActive,
    bool IsFeatured);

public record AdminVariantDto(int Id, string SizeLabel, int SizeSortOrder, string Sku, int Stock);

public record SetStockRequest(int Stock);

public record AdminImageDto(int Id, string PublicId, string AltText, bool IsPrimary, int SortOrder);

public record AdminOrderDto(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    decimal Total,
    int ItemCount,
    string CustomerName,
    string ShippingCity,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    DateTimeOffset CreatedAt)
{
    public IReadOnlyList<OrderStatus> NextStates { get; init; } = [];
}

public record AdminOrderDetailDto(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    string CustomerName,
    string CustomerEmail,
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
    public IReadOnlyList<OrderStatus> NextStates { get; init; } = [];
}

public record SetOrderStatusRequest(OrderStatus Status);

public record LowStockDto(int VariantId, string ProductName, string SizeLabel, string Sku, int Stock);

public record DashboardDto(
    decimal SalesTotal,
    int PaidOrders,
    int PendingOrders,
    int TotalProducts,
    int InactiveProducts,
    int OutOfStockProducts,
    IReadOnlyList<LowStockDto> LowStock);
