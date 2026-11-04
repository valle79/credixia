namespace Lending.Domain.Cash;

public enum CashTransactionType
{
    LoanPayment = 1,        // Pago de préstamo
    LoanDisbursement = 2,   // Desembolso de préstamo
    Adjustment = 3,         // Ajuste manual
    Deposit = 4,            // Depósito bancario
    Withdrawal = 5,         // Retiro bancario
    Expense = 6             // Gasto operativo
}
