using Microsoft.AspNetCore.Identity;

namespace SuperShop.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
