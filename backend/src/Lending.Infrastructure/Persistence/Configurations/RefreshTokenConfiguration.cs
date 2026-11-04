using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id).HasName("pk_refresh_tokens");
        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.TenantId).HasColumnName("tenant_id");
        builder.Property(token => token.FamilyId).HasColumnName("family_id").IsRequired();
        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at");
        builder.Property(token => token.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(token => token.CreatedByIpHash)
            .HasColumnName("created_by_ip_hash")
            .HasMaxLength(64);
        builder.Property(token => token.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(400);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_token_hash");
        builder.HasIndex(token => new { token.UserId, token.FamilyId })
            .HasDatabaseName("ix_refresh_tokens_user_family");
        builder.HasIndex(token => token.ExpiresAt)
            .HasDatabaseName("ix_refresh_tokens_expires_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
