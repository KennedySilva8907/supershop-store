using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperShop.Application.Auth;

namespace SuperShop.Infrastructure.Email;

public class BrevoEmailSender(
    HttpClient client,
    IOptions<EmailOptions> options,
    IConfiguration configuration,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    private string FrontendUrl => configuration["Frontend:Url"] ?? "http://localhost:5173";

    public Task SendEmailConfirmationAsync(
        string email,
        string name,
        string confirmationUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(email, name, "Confirma a tua conta SuperShop",
            EmailTemplates.AccountConfirmation(name, confirmationUrl), cancellationToken);

    public Task SendPasswordResetAsync(
        string email,
        string name,
        string resetUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(email, name, "Recuperar a password SuperShop",
            EmailTemplates.PasswordReset(name, resetUrl), cancellationToken);

    public Task SendOrderConfirmationAsync(
        string email,
        string name,
        OrderEmailSummary order,
        CancellationToken cancellationToken = default) =>
        SendAsync(email, name, $"Encomenda {order.OrderNumber} recebida",
            EmailTemplates.OrderConfirmation(name, ToModel(order)), cancellationToken);

    private OrderEmailModel ToModel(OrderEmailSummary order) => new(
        order.OrderNumber,
        order.Subtotal,
        order.ShippingCost,
        order.Total,
        order.ShippingFullName,
        order.ShippingLine1,
        order.ShippingLine2,
        order.ShippingPostalCode,
        order.ShippingCity,
        [.. order.Lines.Select(l => new OrderLineSummary(l.ProductName, l.SizeLabel, l.Quantity, l.LineTotal))],
        order.PaymentLabel,
        $"{FrontendUrl}/encomenda/{order.OrderNumber}");

    private async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        EmailBody body,
        CancellationToken cancellationToken)
    {
        var (fromName, fromAddress) = _options.ParseFrom();

        var payload = new BrevoMessage(
            new BrevoContact(fromName, fromAddress),
            [new BrevoContact(toName, toEmail)],
            subject,
            body.Html,
            body.Text);

        using var response = await client.PostAsJsonAsync("v3/smtp/email", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogError(
                "Brevo recusou o envio para {Email}. Estado {Status}. Resposta: {Body}",
                toEmail, (int)response.StatusCode, detail);

            throw new InvalidOperationException($"O envio de email falhou com o estado {(int)response.StatusCode}.");
        }

        logger.LogInformation("Email enviado para {Email} com o assunto {Subject}.", toEmail, subject);
    }

    private record BrevoContact(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("email")] string Email);

    private record BrevoMessage(
        [property: JsonPropertyName("sender")] BrevoContact Sender,
        [property: JsonPropertyName("to")] IReadOnlyList<BrevoContact> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("htmlContent")] string HtmlContent,
        [property: JsonPropertyName("textContent")] string TextContent);
}
