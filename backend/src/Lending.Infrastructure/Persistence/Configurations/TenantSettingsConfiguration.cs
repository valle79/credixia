using Lending.Domain.Tenancy;
using Lending.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("tenant_settings");
        builder.HasKey(settings => settings.Id).HasName("pk_tenant_settings");
        builder.Property(settings => settings.Id).HasColumnName("id");
        builder.Property(settings => settings.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(settings => settings.Timezone)
            .HasColumnName("timezone")
            .HasMaxLength(TenantSettings.TimezoneMaxLength)
            .IsRequired();
        builder.Property(settings => settings.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasConversion(currency => currency.Code, value => Currency.FromCode(value))
            .IsRequired();
        builder.Property(settings => settings.Locale)
            .HasColumnName("locale")
            .HasMaxLength(TenantSettings.LocaleMaxLength)
            .IsRequired();
        builder.Property(settings => settings.BusinessDayRule)
            .HasColumnName("business_day_rule")
            .HasMaxLength(TenantSettings.BusinessDayRuleMaxLength)
            .IsRequired();
        builder.Property(settings => settings.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(settings => settings.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(settings => settings.TenantId)
            .IsUnique()
            .HasDatabaseName("ux_tenant_settings_tenant_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(settings => settings.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
