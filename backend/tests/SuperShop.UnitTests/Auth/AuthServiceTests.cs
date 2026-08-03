using SuperShop.Application.Auth;
using SuperShop.Domain.Exceptions;

namespace SuperShop.UnitTests.Auth;

public class AuthServiceTests
{
    [Theory]
    [InlineData("  Kennedy@Example.PT  ", "kennedy@example.pt")]
    [InlineData("ALL@CAPS.COM", "all@caps.com")]
    [InlineData("already@lower.pt", "already@lower.pt")]
    public async Task Register_normalises_the_email(string given, string expected)
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.RegisterAsync(new RegisterRequest(given, "Password123!", "Kennedy", "Silva"));

        Assert.Equal(expected, gateway.LastRegister!.Email);
    }

    [Fact]
    public async Task Login_normalises_the_email_so_case_never_blocks_a_customer()
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.LoginAsync(new LoginRequest("  Kennedy@Example.PT ", "Password123!"));

        Assert.Equal("kennedy@example.pt", gateway.LastLogin!.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refresh_without_a_token_is_unauthorised_and_never_reaches_the_gateway(string? token)
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshAsync(token));
        Assert.Null(gateway.LastRefreshToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Logout_without_a_token_succeeds_quietly(string? token)
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.LogoutAsync(token);

        Assert.Null(gateway.LastLogoutToken);
    }

    [Fact]
    public async Task Forgot_password_normalises_the_email()
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.ForgotPasswordAsync("  Kennedy@Example.PT ");

        Assert.Equal("kennedy@example.pt", gateway.LastForgotEmail);
    }

    [Fact]
    public async Task Update_profile_trims_the_names_before_saving()
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.UpdateProfileAsync("1", new UpdateProfileRequest("  Kennedy  ", "  Silva  ", " 912345678 "));

        Assert.Equal("Kennedy", gateway.LastProfile!.FirstName);
        Assert.Equal("Silva", gateway.LastProfile.LastName);
        Assert.Equal("912345678", gateway.LastProfile.PhoneNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_phone_is_stored_as_nothing_rather_than_as_spaces(string? phone)
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.UpdateProfileAsync("1", new UpdateProfileRequest("Kennedy", "Silva", phone));

        Assert.Null(gateway.LastProfile!.PhoneNumber);
    }

    [Fact]
    public async Task Reusing_the_same_password_is_refused_before_it_reaches_the_gateway()
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ChangePasswordAsync("1", new ChangePasswordRequest("Password123!", "Password123!")));

        Assert.Null(gateway.LastPasswordChange);
    }

    [Fact]
    public async Task A_different_password_goes_through()
    {
        var gateway = new RecordingGateway();
        var service = new AuthService(gateway);

        await service.ChangePasswordAsync("1", new ChangePasswordRequest("Password123!", "Outra456!"));

        Assert.Equal("Outra456!", gateway.LastPasswordChange!.NewPassword);
    }

    private sealed class RecordingGateway : IIdentityGateway
    {
        public RegisterRequest? LastRegister { get; private set; }
        public LoginRequest? LastLogin { get; private set; }
        public string? LastRefreshToken { get; private set; }
        public string? LastLogoutToken { get; private set; }
        public string? LastForgotEmail { get; private set; }
        public UpdateProfileRequest? LastProfile { get; private set; }
        public ChangePasswordRequest? LastPasswordChange { get; private set; }

        private static readonly UserDto Sample =
            new("1", "kennedy@example.pt", "Kennedy", "Silva", null, true, ["Customer"]);

        private static readonly AuthTokens Tokens =
            new("access", DateTimeOffset.UtcNow.AddMinutes(15), "refresh");

        public Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            LastRegister = request;
            return Task.FromResult(Sample);
        }

        public Task<(UserDto User, AuthTokens Tokens)> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            LastLogin = request;
            return Task.FromResult((Sample, Tokens));
        }

        public Task<(UserDto User, AuthTokens Tokens)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LastRefreshToken = refreshToken;
            return Task.FromResult((Sample, Tokens));
        }

        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            LastLogoutToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ResendConfirmationAsync(string email, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
        {
            LastForgotEmail = email;
            return Task.CompletedTask;
        }

        public Task<UserDto> UpdateProfileAsync(
            string userId,
            UpdateProfileRequest request,
            CancellationToken cancellationToken)
        {
            LastProfile = request;
            return Task.FromResult(Sample);
        }

        public Task<(UserDto User, AuthTokens Tokens)> ChangePasswordAsync(
            string userId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            LastPasswordChange = request;
            return Task.FromResult((Sample, Tokens));
        }

        public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<UserDto> GetProfileAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Sample);
    }
}
