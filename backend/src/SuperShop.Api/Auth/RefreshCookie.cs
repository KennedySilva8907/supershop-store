namespace SuperShop.Api.Auth;

public static class RefreshCookie
{
    public const string Name = "supershop_refresh";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Set(HttpContext context, IConfiguration configuration, string token) =>
        context.Response.Cookies.Append(Name, token, Build(context, configuration));

    public static void Delete(HttpContext context, IConfiguration configuration) =>
        context.Response.Cookies.Delete(Name, Build(context, configuration));

    private static CookieOptions Build(HttpContext context, IConfiguration configuration)
    {
        var secure = context.Request.IsHttps;
        var crossSite = configuration.GetValue("Auth:FrontendOnAnotherSite", false);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = crossSite && secure ? SameSiteMode.None : SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
    }
}
