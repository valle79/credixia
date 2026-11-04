namespace Lending.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "lending-api";

    public string Audience { get; set; } = "lending-portal";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        if (SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be at least 32 characters. Configure it through a secret store.");
        }

        if (AccessTokenMinutes is < 5 or > 15)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 5 and 15.");
        }
    }
}
