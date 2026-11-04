using Lending.Domain.Cash;
using Lending.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("cash_sessions");

        builder.HasKey(cs => cs.Id);

        builder.HasIndex(cs => cs.TenantId);
        builder.HasIndex(cs => cs.UserId);
        builder.HasIndex(cs => new { cs.TenantId, cs.UserId, cs.SessionDate }).IsUnique();
        builder.HasIndex(cs => cs.Status);

        builder.Property(cs => cs.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cs => cs.Notes)
            .HasMaxLength(1000);

        // Configure OpeningBalance as owned entity
        builder.OwnsOne(cs => cs.OpeningBalance, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("opening_balance_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("opening_balance_currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure CurrentBalance as owned entity
        builder.OwnsOne(cs => cs.CurrentBalance, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("current_balance_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("current_balance_currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure ClosingBalance as owned entity (optional - set when session is closed)
        builder.OwnsOne(cs => cs.ClosingBalance, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("closing_balance_amount")
                .HasPrecision(18, 2);

            mb.Property(m => m.Currency)
                .HasColumnName("closing_balance_currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3);
        });

        // Configure PhysicalCount as owned entity (nullable until session is closed)
        builder.OwnsOne(cs => cs.PhysicalCount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("physical_count_amount")
                .HasPrecision(18, 2);

            mb.Property(m => m.Currency)
                .HasColumnName("physical_count_currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3);
        });

        // Configure Difference as owned entity (nullable until session is closed)
        builder.OwnsOne(cs => cs.Difference, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("difference_amount")
                .HasPrecision(18, 2);

            mb.Property(m => m.Currency)
                .HasColumnName("difference_currency")
                .HasConversion(c => c.Code, code => Currency.FromCode(code))
                .HasMaxLength(3);
        });

        // Configure Transactions collection as owned entities
        builder.OwnsMany(cs => cs.Transactions, tb =>
        {
            tb.ToTable("cash_transactions");
            tb.HasKey(t => t.Id);
            tb.HasIndex(t => t.SessionId);
            tb.HasIndex(t => t.LoanId);

            tb.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            tb.Property(t => t.Direction)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            tb.Property(t => t.Description)
                .HasMaxLength(500);

            // Configure Amount for CashTransaction
            tb.OwnsOne(t => t.Amount, mb =>
            {
                mb.Property(m => m.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                mb.Property(m => m.Currency)
                    .HasColumnName("currency")
                    .HasConversion(c => c.Code, code => Currency.FromCode(code))
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });
    }
}
