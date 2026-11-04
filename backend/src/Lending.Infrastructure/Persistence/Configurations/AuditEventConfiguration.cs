using Lending.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(auditEvent => auditEvent.Id).HasName("pk_audit_events");
        builder.Property(auditEvent => auditEvent.Id).HasColumnName("id");
        builder.Property(auditEvent => auditEvent.TenantId).HasColumnName("tenant_id");
        builder.Property(auditEvent => auditEvent.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(auditEvent => auditEvent.Action)
            .HasColumnName("action")
            .HasMaxLength(AuditEvent.ActionMaxLength)
            .IsRequired();
        builder.Property(auditEvent => auditEvent.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(AuditEvent.EntityTypeMaxLength)
            .IsRequired();
        builder.Property(auditEvent => auditEvent.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(80);
        builder.Property(auditEvent => auditEvent.BeforeJson).HasColumnName("before_json");
        builder.Property(auditEvent => auditEvent.AfterJson).HasColumnName("after_json");
        builder.Property(auditEvent => auditEvent.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100);
        builder.Property(auditEvent => auditEvent.IpHash)
            .HasColumnName("ip_hash")
            .HasMaxLength(64);
        builder.Property(auditEvent => auditEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.HasIndex(auditEvent => new { auditEvent.TenantId, auditEvent.OccurredAt })
            .HasDatabaseName("ix_audit_events_tenant_occurred");
        builder.HasIndex(auditEvent => new { auditEvent.ActorUserId, auditEvent.OccurredAt })
            .HasDatabaseName("ix_audit_events_actor_occurred");
    }
}
