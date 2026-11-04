using System.Security.Cryptography;
using Lending.Application.Abstractions;

namespace Lending.Infrastructure.Security;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenSize = 32;

    public GeneratedToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSize);
        var token = Base64UrlEncode(bytes);
        return new GeneratedToken(token, Hash(token));
    }

    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.", nameof(token));
        }

        return Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(ToBase64(token))));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ToBase64(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
    }
}
