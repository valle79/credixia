using Lending.Application.Abstractions;
using Lending.Domain.Platform;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Infrastructure.Platform;

public sealed record PlatformSecurityPolicy(
    int MinPasswordLength,
    bool RequireLetterAndDigit,
    int MaxFailedAttempts,
    int LockoutMinutes,
    int RefreshTokenDays);

public sealed record PlatformEmailConfiguration(
    bool Enabled,
    string? Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string? FromAddress,
    string? FromName)
{
    public bool IsReady =>
        Enabled &&
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>
/// Reads (and lazily creates) the single platform settings row. All values persisted here
/// are consumed by real flows: login lockout, password policy, refresh token lifetime and
/// SMTP delivery.
/// </summary>
public sealed class PlatformSettingsStore
{
    private readonly LendingDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ISecretProtector _secretProtector;

    public PlatformSettingsStore(
        LendingDbContext dbContext,
        IClock clock,
        ISecretProtector secretProtector)
    {
        _dbContext = dbContext;
        _clock = clock;
        _secretProtector = secretProtector;
    }

    public async Task<PlatformSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlatformSettings
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = PlatformSettings.Create(_clock.UtcNow);
        _dbContext.PlatformSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task<PlatformSecurityPolicy> GetSecurityPolicyAsync(
        CancellationToken cancellationToken)
    {
        var settings = await GetAsync(cancellationToken);

        return new PlatformSecurityPolicy(
            settings.MinPasswordLength,
            settings.RequireLetterAndDigit,
            settings.MaxFailedAttempts,
            settings.LockoutMinutes,
            settings.RefreshTokenDays);
    }

    public async Task<PlatformEmailConfiguration> GetEmailConfigurationAsync(
        CancellationToken cancellationToken)
    {
        var settings = await GetAsync(cancellationToken);

        string? password = null;
        if (!string.IsNullOrWhiteSpace(settings.EmailPasswordCiphertext))
        {
            password = _secretProtector.Unprotect(settings.EmailPasswordCiphertext);
        }

        return new PlatformEmailConfiguration(
            settings.EmailEnabled,
            settings.EmailHost,
            settings.EmailPort,
            settings.EmailUseSsl,
            settings.EmailUsername,
            password,
            settings.EmailFromAddress,
            settings.EmailFromName);
    }

    public string ProtectEmailPassword(string password) =>
        _secretProtector.Protect(password);
}
