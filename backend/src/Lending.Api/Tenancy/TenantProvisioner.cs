using Lending.Application.Abstractions;
using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Infrastructure.Persistence;

namespace Lending.Api.Tenancy;

public sealed class TenantProvisioner
{
    private readonly LendingDbContext _dbContext;
    private readonly IClock _clock;

    public TenantProvisioner(LendingDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public void AddDefaults(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var now = _clock.UtcNow;
        _dbContext.TenantSettings.Add(TenantSettings.CreateDefault(tenant.Id, now));

        foreach (var definition in SystemRoles.All)
        {
            _dbContext.Roles.Add(Role.CreateForTenant(tenant.Id, definition, now));
        }
    }
}
