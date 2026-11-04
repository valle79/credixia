namespace Lending.Application.Abstractions;

/// <summary>
/// Reversible protection for secrets that must be stored at rest, such as SMTP
/// credentials. Implementations must never return the plaintext through the API.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}
