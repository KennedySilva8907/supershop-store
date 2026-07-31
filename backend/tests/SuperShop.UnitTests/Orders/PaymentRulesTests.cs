using SuperShop.Domain.Orders;

namespace SuperShop.UnitTests.Orders;

public class CardNumberTests
{
    [Theory]
    [InlineData("4539578763621486")]
    [InlineData("4485275742308327")]
    [InlineData("5555555555554444")]
    [InlineData("4111111111111111")]
    [InlineData("378282246310005")]
    [InlineData("4539 5787 6362 1486")]
    [InlineData("4539-5787-6362-1486")]
    public void Valid_numbers_pass_luhn(string number)
    {
        Assert.True(CardNumber.IsValid(number));
    }

    [Theory]
    [InlineData("4539578763621487")]
    [InlineData("1234567812345678")]
    [InlineData("4111111111111112")]
    [InlineData("0000000000000001")]
    public void Numbers_failing_the_checksum_are_rejected(string number)
    {
        Assert.False(CardNumber.IsValid(number));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("4111")]
    [InlineData("41111111111111111111111")]
    [InlineData("4111a111111111111")]
    public void Malformed_input_is_rejected_without_throwing(string? number)
    {
        Assert.False(CardNumber.IsValid(number));
    }

    [Theory]
    [InlineData("4539578763621486", "1486")]
    [InlineData("4539 5787 6362 1486", "1486")]
    [InlineData("378282246310005", "0005")]
    public void Only_the_last_four_digits_are_kept(string number, string expected)
    {
        Assert.Equal(expected, CardNumber.LastFour(number));
    }
}

public class MultibancoReferenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reference_has_nine_digits_and_entity_has_five()
    {
        var details = MultibancoReference.Generate("12345", 42, 99.90m, Now);

        Assert.Equal(5, details.Entity.Length);
        Assert.Equal(9, details.Reference.Length);
        Assert.True(details.Reference.All(char.IsDigit));
        Assert.True(MultibancoReference.IsWellFormed(details.Entity, details.Reference));
    }

    [Fact]
    public void Reference_is_valid_for_forty_eight_hours()
    {
        var details = MultibancoReference.Generate("12345", 42, 99.90m, Now);

        Assert.Equal(Now.AddHours(48), details.ExpiresAt);
        Assert.Equal(48, MultibancoReference.ValidForHours);
    }

    [Fact]
    public void The_same_order_and_amount_always_produce_the_same_reference()
    {
        var first = MultibancoReference.Generate("12345", 42, 99.90m, Now);
        var second = MultibancoReference.Generate("12345", 42, 99.90m, Now.AddMinutes(5));

        Assert.Equal(first.Reference, second.Reference);
    }

    [Fact]
    public void A_different_amount_changes_the_check_digits()
    {
        var first = MultibancoReference.Generate("12345", 42, 99.90m, Now);
        var second = MultibancoReference.Generate("12345", 42, 129.00m, Now);

        Assert.NotEqual(first.Reference, second.Reference);
    }

    [Fact]
    public void Different_orders_get_different_references()
    {
        var first = MultibancoReference.Generate("12345", 42, 99.90m, Now);
        var second = MultibancoReference.Generate("12345", 43, 99.90m, Now);

        Assert.NotEqual(first.Reference, second.Reference);
    }

    [Fact]
    public void Amount_is_rounded_to_two_decimals()
    {
        Assert.Equal(99.90m, MultibancoReference.Generate("12345", 42, 99.895m, Now).Amount);
    }

    [Theory]
    [InlineData(null, "123456789")]
    [InlineData("12345", null)]
    [InlineData("1234", "123456789")]
    [InlineData("12345", "12345678")]
    [InlineData("1234a", "123456789")]
    public void Malformed_pairs_are_rejected(string? entity, string? reference)
    {
        Assert.False(MultibancoReference.IsWellFormed(entity, reference));
    }
}
