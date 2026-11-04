using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("tenant_invitations");
        builder.HasKey(invitation => invitation.Id).HasName("pk_tenant_invitations");
        builder.Property(invitation => invitation.Id).HasColumnName("id");
        builder.Property(invitation => invitation.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(invitation => invitation.Email)
            .HasColumnName("email")
            .HasMaxLength(Lending.Domain.Identity.Email.MaxLength)
            .HasConversion(email => email.Value, value => Lending.Domain.Identity.Email.Create(value))
            .IsRequired();
        builder.Property(invitation => invitation.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(invitation => invitation.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(invitation => invitation.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(invitation => invitation.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();
        builder.Property(invitation => invitation.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(invitation => invitation.RevokedAt).HasColumnName("revoked_at");

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_tenant_invitations_token_hash");
        builder.HasIndex(invitation => new { invitation.TenantId, invitation.Email })
            .HasDatabaseName("ix_tenant_invitations_tenant_email");
        builder.HasIndex(invitation => invitation.ExpiresAt)
            .HasDatabaseName("ix_tenant_invitations_expires_at");

        builder.HasOne<Lending.Domain.Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(invitation => invitation.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(invitation => invitation.Roles)
            .WithOne()
            .HasForeignKey(role => role.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(invitation => invitation.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
