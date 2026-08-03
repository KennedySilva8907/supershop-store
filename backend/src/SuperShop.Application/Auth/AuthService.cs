using SuperShop.Domain.Exceptions;

namespace SuperShop.Application.Auth;

public interface IIdentityGateway
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<(UserDto User, AuthTokens Tokens)> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<(UserDto User, AuthTokens Tokens)> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken);
    Task ResendConfirmationAsync(string email, CancellationToken cancellationToken);
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetProfileAsync(string userId, CancellationToken cancellationToken);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken);

    Task<(UserDto User, AuthTokens Tokens)> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
}

public class AuthService(IIdentityGateway identity)
{
    public Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        identity.RegisterAsync(request with { Email = Normalise(request.Email) }, cancellationToken);

    public Task<(UserDto User, AuthTokens Tokens)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        identity.LoginAsync(request with { Email = Normalise(request.Email) }, cancellationToken);

    public Task<(UserDto User, AuthTokens Tokens)> RefreshAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(refreshToken)
            ? throw new UnauthorizedException("Sessão inválida.")
            : identity.RefreshAsync(refreshToken, cancellationToken);

    public Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(refreshToken)
            ? Task.CompletedTask
            : identity.LogoutAsync(refreshToken, cancellationToken);

    public Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default) =>
        identity.ConfirmEmailAsync(request, cancellationToken);

    public Task ResendConfirmationAsync(string email, CancellationToken cancellationToken = default) =>
        identity.ResendConfirmationAsync(Normalise(email), cancellationToken);

    public Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default) =>
        identity.ForgotPasswordAsync(Normalise(email), cancellationToken);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
        identity.ResetPasswordAsync(request with { Email = Normalise(request.Email) }, cancellationToken);

    public Task<UserDto> GetProfileAsync(string userId, CancellationToken cancellationToken = default) =>
        identity.GetProfileAsync(userId, cancellationToken);

    public Task<UserDto> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default) =>
        identity.UpdateProfileAsync(userId, Clean(request), cancellationToken);

    public Task<(UserDto User, AuthTokens Tokens)> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default) =>
        request.CurrentPassword == request.NewPassword
            ? throw new ConflictException("A password nova tem de ser diferente da atual.")
            : identity.ChangePasswordAsync(userId, request, cancellationToken);

    private static UpdateProfileRequest Clean(UpdateProfileRequest request) => request with
    {
        FirstName = request.FirstName.Trim(),
        LastName = request.LastName.Trim(),
        PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim()
    };

    private static string Normalise(string email) => email.Trim().ToLowerInvariant();
}
