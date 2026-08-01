using Microsoft.Extensions.Logging;
using SuperShop.Application.Auth;

namespace SuperShop.Infrastructure.Email;

public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendEmailConfirmationAsync(
        string email,
        string name,
        string confirmationUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email de confirmação para {Email} ({Name}). Abre este endereço para confirmar: {Url}",
            email, name, confirmationUrl);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(
        string email,
        string name,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Recuperação de password para {Email} ({Name}). Abre este endereço para definir a nova: {Url}",
            email, name, resetUrl);

        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationAsync(
        string email,
        string name,
        OrderEmailSummary order,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Confirmacao da encomenda {OrderNumber} para {Email} ({Name}). Total {Total}, {Lines} linhas, pagamento {Payment}.",
            order.OrderNumber, email, name, order.Total, order.Lines.Count, order.PaymentLabel);

        return Task.CompletedTask;
    }
}
