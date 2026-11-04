using Lending.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class PlatformNotificationConfiguration : IEntityTypeConfiguration<PlatformNotification>
{
    public void Configure(EntityTypeBuilder<PlatformNotification> builder)
    {
        builder.ToTable("platform_notifications");
        builder.HasKey(notification => notification.Id).HasName("pk_platform_notifications");
        builder.Property(notification => notification.Id).HasColumnName("id");

        builder.Property(notification => notification.Title)
            .HasColumnName("title")
            .HasMaxLength(PlatformNotification.TitleMaxLength)
            .IsRequired();
        builder.Property(notification => notification.Message)
            .HasColumnName("message")
            .HasMaxLength(PlatformNotification.MessageMaxLength)
            .IsRequired();
        builder.Property(notification => notification.Category)
            .HasColumnName("category")
            .HasMaxLength(PlatformNotification.CategoryMaxLength)
            .IsRequired();
        builder.Property(notification => notification.Severity)
            .HasColumnName("severity")
            .HasMaxLength(PlatformNotification.SeverityMaxLength)
            .IsRequired();
        builder.Property(notification => notification.LinkUrl)
            .HasColumnName("link_url")
            .HasMaxLength(PlatformNotification.LinkMaxLength);
        builder.Property(notification => notification.IsRead)
            .HasColumnName("is_read")
            .IsRequired();
        builder.Property(notification => notification.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(notification => notification.ReadAt)
            .HasColumnName("read_at");

        builder.HasIndex(notification => notification.CreatedAt)
            .HasDatabaseName("ix_platform_notifications_created_at");
        builder.HasIndex(notification => notification.IsRead)
            .HasDatabaseName("ix_platform_notifications_is_read");
    }
}
