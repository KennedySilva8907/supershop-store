namespace SuperShop.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;

    public (string Name, string Address) ParseFrom()
    {
        var value = From.Trim();
        var open = value.LastIndexOf('<');
        var close = value.LastIndexOf('>');

        if (open < 0 || close < open)
        {
            return (string.Empty, value);
        }

        return (value[..open].Trim(), value[(open + 1)..close].Trim());
    }
}
