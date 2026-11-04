namespace Lending.Domain.LoanProducts;

/// <summary>
/// Frecuencia de pago del préstamo
/// </summary>
public enum PaymentFrequency
{
    Daily = 1,           // Diario
    Weekly = 2,          // Semanal
    Biweekly = 3,        // Quincenal
    Monthly = 4,         // Mensual
    Quarterly = 5,       // Trimestral
    Biannual = 6,        // Semestral
    Annual = 7           // Anual
}
