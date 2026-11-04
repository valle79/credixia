using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(permission => new { permission.RoleId, permission.PermissionCode })
            .HasName("pk_role_permissions");

        builder.Property(permission => permission.RoleId).HasColumnName("role_id");
        builder.Property(permission => permission.PermissionCode)
            .HasColumnName("permission_code")
            .HasMaxLength(80)
            .IsRequired();

        builder.HasOne<Role>()
            .WithMany(role => role.Permissions)
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(permission => permission.PermissionCode)
            .HasDatabaseName("ix_role_permissions_permission_code");
    }
}
