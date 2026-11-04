namespace Lending.Application.Abstractions;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenRequest request);
}

public sealed record AccessTokenRequest(
    Guid UserId,
    string Email,
    bool IsPlatformAdmin,
    Guid? TenantId,
    Guid SessionId,
    string SecurityStamp);

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);
