using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(token => token.Id).HasName("pk_password_reset_tokens");
        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(token => token.UsedAt).HasColumnName("used_at");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_password_reset_tokens_token_hash");
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt })
            .HasDatabaseName("ix_password_reset_tokens_user_expires");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
