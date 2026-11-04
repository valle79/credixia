using Lending.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.DocumentNumber }).IsUnique();

        builder.Property(c => c.DocumentType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.DocumentNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(320);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsMany(c => c.References, rb =>
        {
            rb.ToTable("customer_references");
            rb.HasKey(r => r.Id);
            rb.Property(r => r.FullName).HasMaxLength(200).IsRequired();
            rb.Property(r => r.Relationship).HasMaxLength(100);
            rb.Property(r => r.Phone).HasMaxLength(20).IsRequired();
        });
    }
}
