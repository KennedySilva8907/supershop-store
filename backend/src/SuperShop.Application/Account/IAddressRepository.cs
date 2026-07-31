namespace SuperShop.Application.Account;

public interface IAddressRepository
{
    Task<IReadOnlyList<AddressDto>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<AddressDto> CreateAsync(string userId, SaveAddressRequest request, CancellationToken cancellationToken);

    Task<AddressDto> UpdateAsync(string userId, int id, SaveAddressRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, int id, CancellationToken cancellationToken);
}

public class AddressService(IAddressRepository repository)
{
    public Task<IReadOnlyList<AddressDto>> ListAsync(string userId, CancellationToken cancellationToken = default) =>
        repository.ListAsync(userId, cancellationToken);

    public Task<AddressDto> CreateAsync(
        string userId,
        SaveAddressRequest request,
        CancellationToken cancellationToken = default) =>
        repository.CreateAsync(userId, Clean(request), cancellationToken);

    public Task<AddressDto> UpdateAsync(
        string userId,
        int id,
        SaveAddressRequest request,
        CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(userId, id, Clean(request), cancellationToken);

    public Task DeleteAsync(string userId, int id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(userId, id, cancellationToken);

    private static SaveAddressRequest Clean(SaveAddressRequest request) => request with
    {
        FullName = request.FullName.Trim(),
        Line1 = request.Line1.Trim(),
        Line2 = string.IsNullOrWhiteSpace(request.Line2) ? null : request.Line2.Trim(),
        PostalCode = request.PostalCode.Trim(),
        City = request.City.Trim(),
        Country = string.IsNullOrWhiteSpace(request.Country) ? "PT" : request.Country.Trim().ToUpperInvariant(),
        Phone = request.Phone.Trim()
    };
}
