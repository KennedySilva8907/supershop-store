namespace SuperShop.Domain.Orders;

public static class CardNumber
{
    public static bool IsValid(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return false;
        }

        var digits = number.Where(char.IsDigit).ToArray();

        if (digits.Length is < 12 or > 19 || digits.Length != number.Count(c => !char.IsWhiteSpace(c) && c != '-'))
        {
            return false;
        }

        var sum = 0;
        var doubling = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var value = digits[i] - '0';

            if (doubling)
            {
                value *= 2;

                if (value > 9)
                {
                    value -= 9;
                }
            }

            sum += value;
            doubling = !doubling;
        }

        return sum % 10 == 0;
    }

    public static string LastFour(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());

        return digits.Length < 4 ? digits : digits[^4..];
    }
}
