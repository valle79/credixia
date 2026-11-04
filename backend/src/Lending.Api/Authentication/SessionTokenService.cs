using Lending.Application.Abstractions;
using Lending.Domain.Identity;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Platform;
using Lending.Infrastructure.Security;

namespace Lending.Api.Authentication;

public sealed record IssuedSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid RefreshTokenId,
    Guid SessionId);

public sealed class SessionTokenService
{
    private readonly LendingDbContext _dbContext;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IClock _clock;
    private readonly PlatformSettingsStore _settingsStore;

    public SessionTokenService(
        LendingDbContext dbContext,
        IAccessTokenIssuer accessTokenIssuer,
        ISecureTokenGenerator tokenGenerator,
        IClock clock,
        PlatformSettingsStore settingsStore)
    {
        _dbContext = dbContext;
        _accessTokenIssuer = accessTokenIssuer;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
        _settingsStore = settingsStore;
    }

    public async Task<IssuedSession> CreateAsync(
        User user,
        Guid? tenantId,
        Guid? familyId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        var now = _clock.UtcNow;
        var sessionId = familyId ?? Guid.CreateVersion7();
        var generated = _tokenGenerator.Generate();
        var securityPolicy = await _settingsStore.GetSecurityPolicyAsync(cancellationToken);
        var refreshExpiresAt = now.AddDays(securityPolicy.RefreshTokenDays);
        var boundedUserAgent = userAgent is { Length: > 400 } ? userAgent[..400] : userAgent;

        var refreshToken = RefreshToken.Create(
            user.Id,
            tenantId,
            sessionId,
            generated.Hash,
            now,
            refreshExpiresAt,
            IpHasher.Hash(ipAddress),
            boundedUserAgent);

        _dbContext.RefreshTokens.Add(refreshToken);
        if (saveChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var accessToken = _accessTokenIssuer.Issue(new AccessTokenRequest(
            user.Id,
            user.Email.Value,
            user.IsPlatformAdmin,
            tenantId,
            sessionId,
            user.SecurityStamp));

        return new IssuedSession(
            accessToken.Value,
            accessToken.ExpiresAt,
            generated.Token,
            refreshExpiresAt,
            refreshToken.Id,
            sessionId);
    }
}
