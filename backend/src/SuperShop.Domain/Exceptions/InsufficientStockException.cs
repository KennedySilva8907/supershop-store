namespace SuperShop.Domain.Exceptions;

public class InsufficientStockException(string sku, int requested, int available)
    : DomainException($"Stock insuficiente para {sku}: pedidas {requested}, disponíveis {available}.")
{
    public string Sku { get; } = sku;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}
