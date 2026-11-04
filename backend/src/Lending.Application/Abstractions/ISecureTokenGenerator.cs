namespace Lending.Application.Abstractions;

/// <summary>
/// Generates opaque tokens for refresh, invitations and password resets. Only the hash is
/// persisted; the clear token is returned once to the caller.
/// </summary>
public interface ISecureTokenGenerator
{
    GeneratedToken Generate();

    string Hash(string token);
}

public sealed record GeneratedToken(string Token, string Hash);
