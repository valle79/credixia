using Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class EarlySettlementConfiguration : IEntityTypeConfiguration<EarlySettlement>
{
    public void Configure(EntityTypeBuilder<EarlySettlement> builder)
    {
        builder.ToTable("early_settlements");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.LoanId);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.OwnsOne(e => e.OutstandingPrincipal, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("outstanding_principal")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("outstanding_principal_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(e => e.AccruedInterest, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("accrued_interest")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("accrued_interest_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(e => e.SettlementAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("settlement_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("settlement_amount_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(e => e.AmountPaid, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("amount_paid")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("amount_paid_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
