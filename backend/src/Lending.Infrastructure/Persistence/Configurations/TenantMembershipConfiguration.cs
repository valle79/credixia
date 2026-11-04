using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");
        builder.HasKey(membership => membership.Id).HasName("pk_tenant_memberships");
        builder.Property(membership => membership.Id).HasColumnName("id");
        builder.Property(membership => membership.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(membership => membership.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(membership => membership.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(membership => membership.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(membership => membership.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(membership => new { membership.TenantId, membership.UserId })
            .IsUnique()
            .HasDatabaseName("ux_tenant_memberships_tenant_user");
        builder.HasIndex(membership => membership.UserId)
            .HasDatabaseName("ix_tenant_memberships_user_id");

        builder.HasOne<Lending.Domain.Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(membership => membership.Roles)
            .WithOne()
            .HasForeignKey(role => role.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(membership => membership.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
