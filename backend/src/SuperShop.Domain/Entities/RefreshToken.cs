namespace SuperShop.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public int? ReplacedByTokenId { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
