namespace SuperShop.Domain.Orders;

public record MultibancoDetails(string Entity, string Reference, decimal Amount, DateTimeOffset ExpiresAt);

public static class MultibancoReference
{
    public const int ValidForHours = 48;

    private static readonly int[] Weights = [3, 30, 9, 90, 7, 71, 17, 5, 50, 8, 80, 4, 40, 2, 20, 6, 60, 18, 19, 13];

    public static MultibancoDetails Generate(string entity, int orderId, decimal amount, DateTimeOffset now)
    {
        var body = $"{orderId % 10000000:D7}";
        var checkDigits = CheckDigits(entity, body, amount);

        return new MultibancoDetails(
            entity,
            body + checkDigits,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            now.AddHours(ValidForHours));
    }

    public static bool IsWellFormed(string? entity, string? reference) =>
        entity is { Length: 5 } && entity.All(char.IsDigit) &&
        reference is { Length: 9 } && reference.All(char.IsDigit);

    private static string CheckDigits(string entity, string body, decimal amount)
    {
        var cents = (long)decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero);
        var source = entity + body + $"{cents % 100000000:D8}";

        var sum = 0;

        for (var i = 0; i < source.Length && i < Weights.Length; i++)
        {
            sum += (source[source.Length - 1 - i] - '0') * Weights[i];
        }

        var check = 98 - sum % 97;

        return $"{check % 100:D2}";
    }
}
