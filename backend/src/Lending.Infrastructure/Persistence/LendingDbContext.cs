using Lending.Domain.Audit;
using Lending.Domain.Cash;
using Lending.Domain.Collections;
using Lending.Domain.Customers;
using Lending.Domain.Identity;
using Lending.Domain.LoanProducts;
using Lending.Domain.Loans;
using Lending.Domain.Platform;
using Lending.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lending.Infrastructure.Persistence;

public sealed class LendingDbContext : DbContext
{
    public LendingDbContext(DbContextOptions<LendingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<EarlySettlement> EarlySettlements => Set<EarlySettlement>();

    public DbSet<LoanRefinancing> LoanRefinancings => Set<LoanRefinancing>();

    public DbSet<CashSession> CashSessions => Set<CashSession>();

    public DbSet<CollectionAction> CollectionActions => Set<CollectionAction>();

    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();

    public DbSet<PlatformNotification> PlatformNotifications => Set<PlatformNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LendingDbContext).Assembly);
        modelBuilder.Entity<Tenant>()
            .Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();
        base.OnModelCreating(modelBuilder);
    }
}
