namespace SuperShop.Domain.Exceptions;

public class ForbiddenException(string message) : DomainException(message);
