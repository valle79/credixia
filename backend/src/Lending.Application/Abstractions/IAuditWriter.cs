namespace Lending.Application.Abstractions;

public interface IAuditWriter
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken);
}

public sealed record AuditEntry(
    Guid? TenantId,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string? EntityId = null,
    string? BeforeJson = null,
    string? AfterJson = null,
    string? CorrelationId = null,
    string? IpHash = null);
