using Microsoft.EntityFrameworkCore;
using SuperShop.Application.Account;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Exceptions;

namespace SuperShop.Infrastructure.Persistence.Repositories;

public class AddressRepository(SuperShopDbContext context) : IAddressRepository
{
    public async Task<IReadOnlyList<AddressDto>> ListAsync(string userId, CancellationToken cancellationToken) =>
        await context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Id)
            .Select(a => ToDto(a))
            .ToListAsync(cancellationToken);

    public async Task<AddressDto> CreateAsync(
        string userId,
        SaveAddressRequest request,
        CancellationToken cancellationToken)
    {
        var isFirst = !await context.Addresses.AnyAsync(a => a.UserId == userId, cancellationToken);

        var address = new Address
        {
            UserId = userId,
            FullName = request.FullName,
            Line1 = request.Line1,
            Line2 = request.Line2,
            PostalCode = request.PostalCode,
            City = request.City,
            Country = request.Country,
            Phone = request.Phone,
            IsDefault = request.IsDefault || isFirst
        };

        context.Addresses.Add(address);

        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, address.Id, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return ToDto(address);
    }

    public async Task<AddressDto> UpdateAsync(
        string userId,
        int id,
        SaveAddressRequest request,
        CancellationToken cancellationToken)
    {
        var address = await Owned(userId, id, cancellationToken);

        address.FullName = request.FullName;
        address.Line1 = request.Line1;
        address.Line2 = request.Line2;
        address.PostalCode = request.PostalCode;
        address.City = request.City;
        address.Country = request.Country;
        address.Phone = request.Phone;
        address.IsDefault = request.IsDefault;

        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, id, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return ToDto(address);
    }

    public async Task DeleteAsync(string userId, int id, CancellationToken cancellationToken)
    {
        var address = await Owned(userId, id, cancellationToken);
        var wasDefault = address.IsDefault;

        context.Addresses.Remove(address);
        await context.SaveChangesAsync(cancellationToken);

        if (!wasDefault)
        {
            return;
        }

        var replacement = await context.Addresses
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (replacement is not null)
        {
            replacement.IsDefault = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Address> Owned(string userId, int id, CancellationToken cancellationToken) =>
        await context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
        ?? throw NotFoundException.For("Morada", id);

    private async Task ClearOtherDefaultsAsync(string userId, int keepId, CancellationToken cancellationToken)
    {
        var others = await context.Addresses
            .Where(a => a.UserId == userId && a.Id != keepId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var other in others)
        {
            other.IsDefault = false;
        }
    }

    private static AddressDto ToDto(Address a) => new(
        a.Id, a.FullName, a.Line1, a.Line2, a.PostalCode, a.City, a.Country, a.Phone, a.IsDefault);
}
