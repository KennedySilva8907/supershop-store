using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperShop.Application.Auth;

namespace SuperShop.Infrastructure.Email;

public class BrevoEmailSender(
    HttpClient client,
    IOptions<EmailOptions> options,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendEmailConfirmationAsync(
        string email,
        string name,
        string confirmationUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            name,
            "Confirma a tua conta SuperShop",
            $"""
             <p>Olá {name},</p>
             <p>Confirma o teu email para começares a comprar na SuperShop.</p>
             <p><a href="{confirmationUrl}">Confirmar conta</a></p>
             <p>Se não foste tu a criar esta conta, ignora esta mensagem.</p>
             """,
            cancellationToken);

    public Task SendPasswordResetAsync(
        string email,
        string name,
        string resetUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            name,
            "Recuperar a password SuperShop",
            $"""
             <p>Olá {name},</p>
             <p>Pediste para definir uma nova password. O link é válido durante uma hora.</p>
             <p><a href="{resetUrl}">Definir nova password</a></p>
             <p>Se não foste tu, ignora esta mensagem. A password atual continua válida.</p>
             """,
            cancellationToken);

    private async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string html,
        CancellationToken cancellationToken)
    {
        var (fromName, fromAddress) = _options.ParseFrom();

        var payload = new BrevoMessage(
            new BrevoContact(fromName, fromAddress),
            [new BrevoContact(toName, toEmail)],
            subject,
            html);

        using var response = await client.PostAsJsonAsync("v3/smtp/email", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogError(
                "Brevo recusou o envio para {Email}. Estado {Status}. Resposta: {Body}",
                toEmail, (int)response.StatusCode, body);

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
        [property: JsonPropertyName("htmlContent")] string HtmlContent);
}
