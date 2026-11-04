using Lending.Domain.LoanProducts;
using Lending.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class LoanProductConfiguration : IEntityTypeConfiguration<LoanProduct>
{
    public void Configure(EntityTypeBuilder<LoanProduct> builder)
    {
        builder.ToTable("loan_products");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();

        builder.Property(p => p.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.InterestType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Map MinimumAmount as owned entity with currency
        builder.OwnsOne(p => p.MinimumAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("minimum_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        // Map MaximumAmount - share currency column
        builder.OwnsOne(p => p.MaximumAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("maximum_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            // Use UsingEntity to map to existing currency column
            mb.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        // Ignore FixedCommission for now
        builder.Ignore(p => p.FixedCommission);
    }
}
