using Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans");

        builder.HasKey(l => l.Id);

        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => l.CustomerId);
        builder.HasIndex(l => new { l.TenantId, l.LoanNumber }).IsUnique();
        builder.HasIndex(l => l.Status);

        builder.Property(l => l.LoanNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.InterestType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(l => l.PrincipalAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("principal_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(l => l.TotalInterest, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("total_interest")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("total_interest_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(l => l.TotalAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("total_amount_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(l => l.PaidAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("paid_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("paid_amount_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(l => l.BalanceAmount, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("balance_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            mb.Property(m => m.Currency)
                .HasColumnName("balance_amount_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(l => l.Commission, mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName("commission")
                .HasPrecision(18, 2);

            mb.Property(m => m.Currency)
                .HasColumnName("commission_currency")
                .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                .HasMaxLength(3);
        });

        builder.OwnsMany(l => l.Schedule, sb =>
        {
            sb.ToTable("payment_schedule_items");
            
            sb.HasKey(s => s.Id);
            
            sb.WithOwner()
                .HasForeignKey(s => s.LoanId);
            
            sb.Property(s => s.LoanId)
                .HasColumnName("loan_id")
                .IsRequired();
            
            sb.HasIndex(s => s.LoanId);
            sb.Property(s => s.PaymentNumber).IsRequired();
            sb.Property(s => s.DueDate).IsRequired();
            sb.Property(s => s.PaidDate);

            sb.OwnsOne(s => s.DueAmount, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("due_amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                mb.Property(m => m.Currency)
                    .HasColumnName("currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3)
                    .IsRequired();
            });

            sb.OwnsOne(s => s.PaidAmount, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("paid_amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                mb.Property(m => m.Currency)
                    .HasColumnName("paid_amount_currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3)
                    .IsRequired();
            });

            sb.OwnsOne(s => s.LateFee, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("late_fee")
                    .HasPrecision(18, 2)
                    .IsRequired();

                mb.Property(m => m.Currency)
                    .HasColumnName("late_fee_currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3)
                    .IsRequired();
            });

            sb.OwnsOne(s => s.PrincipalAmount, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("principal_amount")
                    .HasPrecision(18, 2);

                mb.Property(m => m.Currency)
                    .HasColumnName("principal_currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3);
            });

            sb.OwnsOne(s => s.InterestAmount, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("interest_amount")
                    .HasPrecision(18, 2);

                mb.Property(m => m.Currency)
                    .HasColumnName("interest_currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3);
            });

            sb.OwnsOne(s => s.RemainingBalance, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("remaining_balance")
                    .HasPrecision(18, 2);

                mb.Property(m => m.Currency)
                    .HasColumnName("remaining_balance_currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3);
            });
        });

        builder.OwnsMany(l => l.Payments, pb =>
        {
            pb.ToTable("payments");
            
            pb.HasKey(p => p.Id);
            
            pb.WithOwner()
                .HasForeignKey(p => p.LoanId);
            
            pb.Property(p => p.LoanId)
                .HasColumnName("loan_id")
                .IsRequired();
            
            pb.HasIndex(p => p.LoanId);
            pb.Property(p => p.ReceiptNumber).HasMaxLength(50).IsRequired();
            pb.Property(p => p.PaymentDate).IsRequired();
            pb.Property(p => p.ReceivedByUserId).IsRequired();
            pb.Property(p => p.RegisteredAt).IsRequired();

            pb.OwnsOne(p => p.Amount, mb =>
            {
                mb.WithOwner();
                
                mb.Property(m => m.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                mb.Property(m => m.Currency)
                    .HasColumnName("currency")
                    .HasConversion(c => c.Code, code => Lending.Domain.ValueObjects.Currency.FromCode(code))
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });
    }
}
