using Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class LoanRefinancingConfiguration : IEntityTypeConfiguration<LoanRefinancing>
{
    public void Configure(EntityTypeBuilder<LoanRefinancing> builder)
    {
        builder.ToTable("loan_refinancings");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.OriginalLoanId);
        builder.HasIndex(r => r.NewLoanId);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.OwnsOne(r => r.OutstandingPrincipal, mb =>
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

        builder.OwnsOne(r => r.AccruedInterest, mb =>
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

        builder.OwnsOne(r => r.TotalDebt, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("total_debt")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("total_debt_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(r => r.NewPrincipal, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("new_principal")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("new_principal_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
