using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class MembershipRoleConfiguration : IEntityTypeConfiguration<MembershipRole>
{
    public void Configure(EntityTypeBuilder<MembershipRole> builder)
    {
        builder.ToTable("membership_roles");
        builder.HasKey(role => new { role.MembershipId, role.RoleId })
            .HasName("pk_membership_roles");

        builder.Property(role => role.MembershipId).HasColumnName("membership_id");
        builder.Property(role => role.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(role => role.RoleId).HasColumnName("role_id");

        builder.HasOne(role => role.Role)
            .WithMany()
            .HasForeignKey(role => role.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(role => role.TenantId)
            .HasDatabaseName("ix_membership_roles_tenant_id");
    }
}
