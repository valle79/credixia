using Lending.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tenants");

        builder.HasKey(tenant => tenant.Id).HasName("pk_tenants");
        builder.Property(tenant => tenant.Id).HasColumnName("id");

        builder.Property(tenant => tenant.Slug)
            .HasColumnName("slug")
            .HasMaxLength(TenantSlug.MaxLength)
            .HasConversion(
                slug => slug.Value,
                value => TenantSlug.Create(value))
            .IsRequired();

        builder.Property(tenant => tenant.Name)
            .HasColumnName("name")
            .HasMaxLength(Tenant.NameMaxLength)
            .IsRequired();

        builder.Property(tenant => tenant.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(tenant => tenant.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(tenant => tenant.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(tenant => tenant.Slug)
            .IsUnique()
            .HasDatabaseName("ux_tenants_slug");
    }
}
