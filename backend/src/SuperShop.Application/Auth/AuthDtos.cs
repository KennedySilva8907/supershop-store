namespace SuperShop.Application.Auth;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName);

public record LoginRequest(string Email, string Password);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ForgotPasswordRequest(string Email);

public record ConfirmEmailRequest(string UserId, string Token);

public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AuthTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken);

public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles);

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
