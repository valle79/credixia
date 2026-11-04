using Lending.Api.Configuration;
using Lending.Application.Abstractions;
using Lending.Domain.Identity;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lending.Api.Infrastructure;

/// <summary>
/// Creates the first platform administrator from environment configuration. It is
/// idempotent and refuses to overwrite an existing account.
/// </summary>
public sealed class PlatformAdminSeeder
{
    private readonly LendingDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly BootstrapOptions _options;
    private readonly ILogger<PlatformAdminSeeder> _logger;

    public PlatformAdminSeeder(
        LendingDbContext dbContext,
        IPasswordHasher passwordHasher,
        IClock clock,
        IOptions<BootstrapOptions> options,
        ILogger<PlatformAdminSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return;
        }

        if (_options.Password.Length < 12)
        {
            throw new InvalidOperationException(
                "Bootstrap:Password must be at least 12 characters.");
        }

        var email = Email.Create(_options.Email);
        var existing = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsPlatformAdmin)
            {
                _logger.LogWarning(
                    "Bootstrap user {Email} already exists without platform administrator rights. Skipping.",
                    email.Value);
                return;
            }

            _logger.LogInformation(
                "Bootstrap platform administrator {Email} already exists.",
                email.Value);
            return;
        }

        var now = _clock.UtcNow;
        var user = User.Create(
            email,
            _options.FullName,
            _passwordHasher.Hash(_options.Password),
            now,
            isPlatformAdmin: true);

        user.VerifyEmail(now);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bootstrap platform administrator {Email} created.",
            email.Value);
    }
}
