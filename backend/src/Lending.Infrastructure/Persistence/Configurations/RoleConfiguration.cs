using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(role => role.Id).HasName("pk_roles");
        builder.Property(role => role.Id).HasColumnName("id");
        builder.Property(role => role.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(role => role.Code)
            .HasColumnName("code")
            .HasMaxLength(Role.CodeMaxLength)
            .IsRequired();
        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasMaxLength(Role.NameMaxLength)
            .IsRequired();
        builder.Property(role => role.IsSystem).HasColumnName("is_system").IsRequired();
        builder.Property(role => role.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(role => new { role.TenantId, role.Code })
            .IsUnique()
            .HasDatabaseName("ux_roles_tenant_code");

        builder.HasOne<Lending.Domain.Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(role => role.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(role => role.Permissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
