using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Loans;

/// <summary>
/// Registro de una operación de refinanciamiento
/// Vincula el préstamo original con el nuevo préstamo
/// </summary>
public sealed class LoanRefinancing : Entity
{
    private LoanRefinancing()
    {
    }

    private LoanRefinancing(
        Guid tenantId,
        Guid originalLoanId,
        Guid newLoanId,
        Money outstandingPrincipal,
        Money accruedInterest,
        Money totalDebt,
        Money newPrincipal,
        decimal newInterestRate,
        DateOnly refinancingDate,
        string? notes,
        DateTimeOffset now)
    {
        TenantId = tenantId;
        OriginalLoanId = originalLoanId;
        NewLoanId = newLoanId;
        OutstandingPrincipal = outstandingPrincipal;
        AccruedInterest = accruedInterest;
        TotalDebt = totalDebt;
        NewPrincipal = newPrincipal;
        NewInterestRate = newInterestRate;
        RefinancingDate = refinancingDate;
        Notes = notes;
        CreatedAt = now;
    }

    public Guid TenantId { get; private set; }

    /// <summary>
    /// ID del préstamo original que se refinancia
    /// </summary>
    public Guid OriginalLoanId { get; private set; }

    /// <summary>
    /// ID del nuevo préstamo creado
    /// </summary>
    public Guid NewLoanId { get; private set; }

    /// <summary>
    /// Capital pendiente del préstamo original
    /// </summary>
    public Money OutstandingPrincipal { get; private set; } = null!;

    /// <summary>
    /// Interés devengado hasta la fecha de refinanciamiento
    /// </summary>
    public Money AccruedInterest { get; private set; } = null!;

    /// <summary>
    /// Deuda total refinanciada (capital + interés devengado)
    /// </summary>
    public Money TotalDebt { get; private set; } = null!;

    /// <summary>
    /// Monto principal del nuevo préstamo (puede incluir capital adicional)
    /// </summary>
    public Money NewPrincipal { get; private set; } = null!;

    /// <summary>
    /// Tasa de interés del nuevo préstamo
    /// </summary>
    public decimal NewInterestRate { get; private set; }

    /// <summary>
    /// Fecha en que se realizó el refinanciamiento
    /// </summary>
    public DateOnly RefinancingDate { get; private set; }

    /// <summary>
    /// Notas adicionales sobre el refinanciamiento
    /// </summary>
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static LoanRefinancing Create(
        Guid tenantId,
        Guid originalLoanId,
        Guid newLoanId,
        Money outstandingPrincipal,
        Money accruedInterest,
        Money totalDebt,
        Money newPrincipal,
        decimal newInterestRate,
        DateOnly refinancingDate,
        string? notes,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (originalLoanId == Guid.Empty)
        {
            throw new ArgumentException("Original loan identifier cannot be empty.", nameof(originalLoanId));
        }

        if (newLoanId == Guid.Empty)
        {
            throw new ArgumentException("New loan identifier cannot be empty.", nameof(newLoanId));
        }

        if (outstandingPrincipal == null || outstandingPrincipal.Amount <= 0)
        {
            throw new DomainException("refinancing.principal.invalid", "El capital pendiente debe ser mayor a cero.");
        }

        if (accruedInterest == null || accruedInterest.Amount < 0)
        {
            throw new DomainException("refinancing.interest.invalid", "El interés devengado no puede ser negativo.");
        }

        if (totalDebt == null || totalDebt.Amount <= 0)
        {
            throw new DomainException("refinancing.debt.invalid", "La deuda total debe ser mayor a cero.");
        }

        if (newPrincipal == null || newPrincipal.Amount < totalDebt.Amount)
        {
            throw new DomainException("refinancing.new_principal.insufficient", 
                "El principal del nuevo préstamo debe cubrir al menos la deuda total.");
        }

        return new LoanRefinancing(
            tenantId,
            originalLoanId,
            newLoanId,
            outstandingPrincipal,
            accruedInterest,
            totalDebt,
            newPrincipal,
            newInterestRate,
            refinancingDate,
            notes,
            now);
    }
}
