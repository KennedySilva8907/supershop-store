namespace SuperShop.Application.Orders;

public interface IOrderRepository
{
    Task<OrderDto> PlaceAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken);

    Task<OrderDto> ConfirmPaymentAsync(string userId, string orderNumber, CancellationToken cancellationToken);

    Task<OrderDto> CancelAsync(string userId, string orderNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderSummaryDto>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<OrderDto> GetAsync(string userId, string orderNumber, CancellationToken cancellationToken);
}

public class OrderService(IOrderRepository repository)
{
    public Task<OrderDto> PlaceAsync(
        string userId,
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default) =>
        repository.PlaceAsync(userId, request, cancellationToken);

    public Task<OrderDto> ConfirmPaymentAsync(
        string userId,
        string orderNumber,
        CancellationToken cancellationToken = default) =>
        repository.ConfirmPaymentAsync(userId, orderNumber, cancellationToken);

    public Task<OrderDto> CancelAsync(
        string userId,
        string orderNumber,
        CancellationToken cancellationToken = default) =>
        repository.CancelAsync(userId, orderNumber, cancellationToken);

    public Task<IReadOnlyList<OrderSummaryDto>> ListAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(userId, cancellationToken);

    public Task<OrderDto> GetAsync(
        string userId,
        string orderNumber,
        CancellationToken cancellationToken = default) =>
        repository.GetAsync(userId, orderNumber, cancellationToken);
}
