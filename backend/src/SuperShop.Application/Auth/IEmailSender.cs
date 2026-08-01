namespace SuperShop.Application.Auth;

public record OrderEmailLine(string ProductName, string SizeLabel, int Quantity, decimal LineTotal);

public record OrderEmailSummary(
    string OrderNumber,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string ShippingFullName,
    string ShippingLine1,
    string? ShippingLine2,
    string ShippingPostalCode,
    string ShippingCity,
    IReadOnlyList<OrderEmailLine> Lines,
    string PaymentLabel);

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(
        string email,
        string name,
        string confirmationUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string email,
        string name,
        string resetUrl,
        CancellationToken cancellationToken = default);

    Task SendOrderConfirmationAsync(
        string email,
        string name,
        OrderEmailSummary order,
        CancellationToken cancellationToken = default);
}
