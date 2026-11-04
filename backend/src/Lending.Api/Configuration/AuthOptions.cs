namespace Lending.Api.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string RefreshCookieName { get; set; } = "lending_refresh";

    public int RefreshTokenDays { get; set; } = 14;

    public int InvitationDays { get; set; } = 3;

    public int PasswordResetMinutes { get; set; } = 30;

    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int MinPasswordLength { get; set; } = 12;

    public string PublicUrl { get; set; } = "http://localhost:5173";

    public void Validate()
    {
        if (RefreshTokenDays is < 1 or > 90)
        {
            throw new InvalidOperationException("Auth:RefreshTokenDays must be between 1 and 90.");
        }

        if (InvitationDays is < 1 or > 14)
        {
            throw new InvalidOperationException("Auth:InvitationDays must be between 1 and 14.");
        }

        if (PasswordResetMinutes is < 5 or > 60)
        {
            throw new InvalidOperationException("Auth:PasswordResetMinutes must be between 5 and 60.");
        }

        if (MinPasswordLength is < 10 or > 128)
        {
            throw new InvalidOperationException("Auth:MinPasswordLength must be between 10 and 128.");
        }

        if (!Uri.TryCreate(PublicUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Auth:PublicUrl must be an absolute URL.");
        }
    }
}
