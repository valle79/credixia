using Lending.Application.Abstractions;
using Lending.Domain.Audit;
using Lending.Infrastructure.Persistence;

namespace Lending.Infrastructure.Audit;

public sealed class AuditWriter : IAuditWriter
{
    private readonly LendingDbContext _dbContext;
    private readonly IClock _clock;

    public AuditWriter(LendingDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var auditEvent = AuditEvent.Create(
            entry.TenantId,
            entry.ActorUserId,
            entry.Action,
            entry.EntityType,
            _clock.UtcNow,
            entry.EntityId,
            entry.BeforeJson,
            entry.AfterJson,
            entry.CorrelationId,
            entry.IpHash);

        _dbContext.AuditEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
