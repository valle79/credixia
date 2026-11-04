using Lending.Domain.Common;

namespace Lending.Domain.Audit;

/// <summary>
/// Append-only record of a security or business-critical action. There is deliberately no
/// update or delete method.
/// </summary>
public sealed class AuditEvent : Entity
{
    public const int ActionMaxLength = 120;
    public const int EntityTypeMaxLength = 80;

    private AuditEvent()
    {
    }

    private AuditEvent(
        Guid? tenantId,
        Guid? actorUserId,
        string action,
        string entityType,
        string? entityId,
        string? beforeJson,
        string? afterJson,
        string? correlationId,
        string? ipHash,
        DateTimeOffset occurredAt)
    {
        TenantId = tenantId;
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        CorrelationId = correlationId;
        IpHash = ipHash;
        OccurredAt = occurredAt;
    }

    public Guid? TenantId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string? EntityId { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? IpHash { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditEvent Create(
        Guid? tenantId,
        Guid? actorUserId,
        string action,
        string entityType,
        DateTimeOffset occurredAt,
        string? entityId = null,
        string? beforeJson = null,
        string? afterJson = null,
        string? correlationId = null,
        string? ipHash = null)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > ActionMaxLength)
        {
            throw new ArgumentException("Audit action is required and must be bounded.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityType) || entityType.Length > EntityTypeMaxLength)
        {
            throw new ArgumentException("Audit entity type is required and must be bounded.", nameof(entityType));
        }

        if (occurredAt == default)
        {
            throw new ArgumentException("Audit timestamp is required.", nameof(occurredAt));
        }

        return new AuditEvent(
            tenantId,
            actorUserId,
            action,
            entityType,
            entityId,
            beforeJson,
            afterJson,
            correlationId,
            ipHash,
            occurredAt);
    }
}
