using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id).HasName("pk_users");
        builder.Property(user => user.Id).HasColumnName("id");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(Lending.Domain.Identity.Email.MaxLength)
            .HasConversion(email => email.Value, value => Lending.Domain.Identity.Email.Create(value))
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.IsPlatformAdmin)
            .HasColumnName("is_platform_admin")
            .IsRequired();

        builder.Property(user => user.EmailVerifiedAt).HasColumnName("email_verified_at");
        builder.Property(user => user.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(user => user.FailedAccessCount)
            .HasColumnName("failed_access_count")
            .IsRequired();
        builder.Property(user => user.LockoutUntil).HasColumnName("lockout_until");
        builder.Property(user => user.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(user => user.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");
        builder.HasIndex(user => user.Status)
            .HasDatabaseName("ix_users_status");
    }
}
