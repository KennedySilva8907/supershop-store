namespace SuperShop.Application.Auth;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string email, string name, string confirmationUrl, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string email, string name, string resetUrl, CancellationToken cancellationToken = default);
}
