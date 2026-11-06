using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Loans;

/// <summary>
/// Registro de una liquidación anticipada
/// Documenta los cálculos y montos de la liquidación
/// </summary>
public sealed class EarlySettlement : Entity
{
    private EarlySettlement()
    {
    }

    private EarlySettlement(
        Guid tenantId,
        Guid loanId,
        Money outstandingPrincipal,
        Money accruedInterest,
        Money settlementAmount,
        Money amountPaid,
        DateOnly settlementDate,
        string? notes,
        DateTimeOffset now)
    {
        TenantId = tenantId;
        LoanId = loanId;
        OutstandingPrincipal = outstandingPrincipal;
        AccruedInterest = accruedInterest;
        SettlementAmount = settlementAmount;
        AmountPaid = amountPaid;
        SettlementDate = settlementDate;
        Notes = notes;
        CreatedAt = now;
    }

    public Guid TenantId { get; private set; }

    /// <summary>
    /// ID del préstamo liquidado
    /// </summary>
    public Guid LoanId { get; private set; }

    /// <summary>
    /// Capital pendiente al momento de la liquidación
    /// </summary>
    public Money OutstandingPrincipal { get; private set; } = null!;

    /// <summary>
    /// Interés devengado hasta la fecha de liquidación
    /// </summary>
    public Money AccruedInterest { get; private set; } = null!;

    /// <summary>
    /// Monto calculado para liquidación (capital + interés devengado)
    /// </summary>
    public Money SettlementAmount { get; private set; } = null!;

    /// <summary>
    /// Monto real pagado por el cliente
    /// </summary>
    public Money AmountPaid { get; private set; } = null!;

    /// <summary>
    /// Fecha en que se realizó la liquidación
    /// </summary>
    public DateOnly SettlementDate { get; private set; }

    /// <summary>
    /// Notas adicionales sobre la liquidación
    /// </summary>
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static EarlySettlement Create(
        Guid tenantId,
        Guid loanId,
        Money outstandingPrincipal,
        Money accruedInterest,
        Money settlementAmount,
        Money amountPaid,
        DateOnly settlementDate,
        string? notes,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (loanId == Guid.Empty)
        {
            throw new ArgumentException("Loan identifier cannot be empty.", nameof(loanId));
        }

        if (outstandingPrincipal == null || outstandingPrincipal.Amount <= 0)
        {
            throw new DomainException("settlement.principal.invalid", "El capital pendiente debe ser mayor a cero.");
        }

        if (accruedInterest == null || accruedInterest.Amount < 0)
        {
            throw new DomainException("settlement.interest.invalid", "El interés devengado no puede ser negativo.");
        }

        if (settlementAmount == null || settlementAmount.Amount <= 0)
        {
            throw new DomainException("settlement.amount.invalid", "El monto de liquidación debe ser mayor a cero.");
        }

        if (amountPaid == null || amountPaid.Amount < settlementAmount.Amount)
        {
            throw new DomainException("settlement.paid.insufficient", 
                "El monto pagado debe ser al menos igual al monto de liquidación.");
        }

        return new EarlySettlement(
            tenantId,
            loanId,
            outstandingPrincipal,
            accruedInterest,
            settlementAmount,
            amountPaid,
            settlementDate,
            notes,
            now);
    }
}
