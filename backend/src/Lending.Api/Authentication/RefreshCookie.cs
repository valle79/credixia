namespace Lending.Api.Authentication;

public static class RefreshCookie
{
    public const string Path = "/api/v1/auth";

    public static string? Read(HttpContext context, string cookieName) =>
        context.Request.Cookies.TryGetValue(cookieName, out var value) ? value : null;

    public static void Append(
        HttpContext context,
        string cookieName,
        string token,
        DateTimeOffset expiresAt)
    {
        context.Response.Cookies.Append(
            cookieName,
            token,
            CreateOptions(context, expiresAt));
    }

    public static void Delete(HttpContext context, string cookieName)
    {
        context.Response.Cookies.Delete(cookieName, CreateOptions(context, null));
    }

    private static CookieOptions CreateOptions(HttpContext context, DateTimeOffset? expiresAt) =>
        new()
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = Path,
            IsEssential = true,
            Expires = expiresAt,
        };
}
