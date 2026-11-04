using Lending.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSettings> builder)
    {
        builder.ToTable("platform_settings");
        builder.HasKey(settings => settings.Id).HasName("pk_platform_settings");
        builder.Property(settings => settings.Id).HasColumnName("id");

        builder.Property(settings => settings.MinPasswordLength)
            .HasColumnName("min_password_length")
            .IsRequired();
        builder.Property(settings => settings.RequireLetterAndDigit)
            .HasColumnName("require_letter_and_digit")
            .IsRequired();
        builder.Property(settings => settings.MaxFailedAttempts)
            .HasColumnName("max_failed_attempts")
            .IsRequired();
        builder.Property(settings => settings.LockoutMinutes)
            .HasColumnName("lockout_minutes")
            .IsRequired();
        builder.Property(settings => settings.RefreshTokenDays)
            .HasColumnName("refresh_token_days")
            .IsRequired();

        builder.Property(settings => settings.EmailEnabled)
            .HasColumnName("email_enabled")
            .IsRequired();
        builder.Property(settings => settings.EmailHost)
            .HasColumnName("email_host")
            .HasMaxLength(PlatformSettings.EmailHostMaxLength);
        builder.Property(settings => settings.EmailPort)
            .HasColumnName("email_port")
            .IsRequired();
        builder.Property(settings => settings.EmailUseSsl)
            .HasColumnName("email_use_ssl")
            .IsRequired();
        builder.Property(settings => settings.EmailUsername)
            .HasColumnName("email_username")
            .HasMaxLength(PlatformSettings.EmailUsernameMaxLength);
        builder.Property(settings => settings.EmailPasswordCiphertext)
            .HasColumnName("email_password_ciphertext")
            .HasMaxLength(PlatformSettings.EmailPasswordCipherMaxLength);
        builder.Property(settings => settings.EmailFromAddress)
            .HasColumnName("email_from_address")
            .HasMaxLength(PlatformSettings.EmailAddressMaxLength);
        builder.Property(settings => settings.EmailFromName)
            .HasColumnName("email_from_name")
            .HasMaxLength(PlatformSettings.EmailDisplayNameMaxLength);
        builder.Property(settings => settings.EmailLastTestAt)
            .HasColumnName("email_last_test_at");
        builder.Property(settings => settings.EmailLastTestSucceeded)
            .HasColumnName("email_last_test_succeeded");
        builder.Property(settings => settings.EmailLastTestError)
            .HasColumnName("email_last_test_error")
            .HasMaxLength(PlatformSettings.EmailErrorMaxLength);

        builder.Property(settings => settings.NotifyTenantCreated)
            .HasColumnName("notify_tenant_created")
            .IsRequired();
        builder.Property(settings => settings.NotifyTenantSuspended)
            .HasColumnName("notify_tenant_suspended")
            .IsRequired();
        builder.Property(settings => settings.NotifyTenantReactivated)
            .HasColumnName("notify_tenant_reactivated")
            .IsRequired();
        builder.Property(settings => settings.NotifySuspiciousAccess)
            .HasColumnName("notify_suspicious_access")
            .IsRequired();
        builder.Property(settings => settings.NotifyEmailFailures)
            .HasColumnName("notify_email_failures")
            .IsRequired();

        builder.Property(settings => settings.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(settings => settings.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Ignore(settings => settings.HasEmailPassword);
    }
}
