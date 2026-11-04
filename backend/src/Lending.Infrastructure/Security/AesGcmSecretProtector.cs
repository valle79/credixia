using System.Security.Cryptography;
using System.Text;
using Lending.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Lending.Infrastructure.Security;

/// <summary>
/// AES-GCM protection for platform secrets. The key comes from
/// Security:SecretEncryptionKey when configured; otherwise it is derived from the JWT
/// signing key so local development works without an extra secret.
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string Prefix = "v1.";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmSecretProtector(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredKey = configuration["Security:SecretEncryptionKey"];
        var fallbackKey = configuration["Jwt:SigningKey"];

        _key = ResolveKey(configuredKey, fallbackKey);
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException("Secret value is required.", nameof(plaintext));
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        ciphertext.CopyTo(payload, NonceSize + TagSize);

        return Prefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue) ||
            !protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Protected secret has an unsupported format.");
        }

        var payload = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Protected secret is truncated.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] ResolveKey(string? configuredKey, string? fallbackKey)
    {
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            try
            {
                var decoded = Convert.FromBase64String(configuredKey);
                if (decoded.Length == 32)
                {
                    return decoded;
                }
            }
            catch (FormatException)
            {
                // Fall through to the derived key.
            }
        }

        if (string.IsNullOrWhiteSpace(fallbackKey))
        {
            throw new InvalidOperationException(
                "Security:SecretEncryptionKey or Jwt:SigningKey is required to protect platform secrets.");
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(fallbackKey));
    }
}
