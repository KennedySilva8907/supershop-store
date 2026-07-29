namespace SuperShop.Domain.Entities;

public class Address
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Line1 { get; set; } = null!;
    public string? Line2 { get; set; }
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Country { get; set; } = "PT";
    public string Phone { get; set; } = null!;
    public bool IsDefault { get; set; }
}
