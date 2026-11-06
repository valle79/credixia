namespace Lending.Domain.Loans;

public enum LoanStatus
{
    Pending = 1,      // Pendiente de aprobación
    Approved = 2,     // Aprobado, pendiente de desembolso
    Active = 3,       // Activo (desembolsado)
    PaidOff = 4,      // Pagado completamente
    Rejected = 5,     // Rechazado
    Cancelled = 6,    // Cancelado
    DefaultedDef = 7, // En mora/incobrable
    Refinanced = 8,   // Refinanciado (reemplazado por nuevo préstamo)
    SettledEarly = 9  // Liquidado anticipadamente
}
