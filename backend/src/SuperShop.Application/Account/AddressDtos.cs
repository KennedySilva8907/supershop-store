namespace SuperShop.Application.Account;

public record AddressDto(
    int Id,
    string FullName,
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string Country,
    string Phone,
    bool IsDefault);

public record SaveAddressRequest(
    string FullName,
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string Country,
    string Phone,
    bool IsDefault);

public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
