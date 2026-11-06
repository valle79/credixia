namespace Lending.Domain.LoanProducts;

/// <summary>
/// Tipo de cálculo de interés
/// </summary>
public enum InterestType
{
    Simple = 1,          // Interés plano: tasa aplicada sobre el capital total
    Compound = 2,        // Interés compuesto (compatibilidad)
    French = 3           // Amortización francesa: cuota fija, interés sobre saldo
}
