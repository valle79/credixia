namespace Lending.Domain.Identity;

/// <summary>
/// Stable role codes. Display names live in the presentation layer so that renaming a role
/// never changes authorization behavior.
/// </summary>
public static class RoleCodes
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string CreditAnalyst = "CreditAnalyst";
    public const string CreditApprover = "CreditApprover";
    public const string Cashier = "Cashier";
    public const string Collector = "Collector";
    public const string Auditor = "Auditor";
}
