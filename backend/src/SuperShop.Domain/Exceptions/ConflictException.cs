namespace SuperShop.Domain.Exceptions;

public class ConflictException(string message) : DomainException(message);
