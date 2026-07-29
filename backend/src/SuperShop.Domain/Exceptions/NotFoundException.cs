namespace SuperShop.Domain.Exceptions;

public class NotFoundException(string message) : DomainException(message)
{
    public static NotFoundException For(string resource, object key) =>
        new($"{resource} '{key}' não foi encontrado.");
}
