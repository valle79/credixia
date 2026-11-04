using Lending.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class InvitationRoleConfiguration : IEntityTypeConfiguration<InvitationRole>
{
    public void Configure(EntityTypeBuilder<InvitationRole> builder)
    {
        builder.ToTable("tenant_invitation_roles");
        builder.HasKey(role => new { role.InvitationId, role.RoleCode })
            .HasName("pk_tenant_invitation_roles");

        builder.Property(role => role.InvitationId).HasColumnName("invitation_id");
        builder.Property(role => role.RoleCode)
            .HasColumnName("role_code")
            .HasMaxLength(Role.CodeMaxLength)
            .IsRequired();
    }
}
