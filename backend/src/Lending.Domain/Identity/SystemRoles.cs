namespace Lending.Domain.Identity;

/// <summary>
/// Default role catalog created for every tenant. Owners and admins can later adjust
/// custom roles, but these system roles remain available.
/// </summary>
public static class SystemRoles
{
    private static readonly string[] ReadOnly =
    [
        Permissions.MembersView,
        Permissions.RolesView,
        Permissions.SettingsView,
        Permissions.CustomersView,
        Permissions.LoansView,
        Permissions.ReportsView,
        Permissions.AuditView,
    ];

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new(
            RoleCodes.Owner,
            "Propietario",
            [
                Permissions.MembersView,
                Permissions.MembersManage,
                Permissions.RolesView,
                Permissions.SettingsView,
                Permissions.SettingsManage,
                Permissions.AuditView,
                Permissions.CustomersView,
                Permissions.CustomersManage,
                Permissions.LoansView,
                Permissions.LoansManage,
                Permissions.PaymentsManage,
                Permissions.CashManage,
                Permissions.CollectionsManage,
                Permissions.ReportsView,
            ]),
        new(
            RoleCodes.Admin,
            "Administrador",
            [
                Permissions.MembersView,
                Permissions.MembersManage,
                Permissions.RolesView,
                Permissions.SettingsView,
                Permissions.SettingsManage,
                Permissions.AuditView,
                Permissions.CustomersView,
                Permissions.CustomersManage,
                Permissions.LoansView,
                Permissions.LoansManage,
                Permissions.PaymentsManage,
                Permissions.CashManage,
                Permissions.CollectionsManage,
                Permissions.ReportsView,
            ]),
        new(
            RoleCodes.CreditAnalyst,
            "Analista de créditos",
            [
                Permissions.CustomersView,
                Permissions.CustomersManage,
                Permissions.LoansView,
                Permissions.ReportsView,
            ]),
        new(
            RoleCodes.CreditApprover,
            "Aprobador de créditos",
            [
                Permissions.CustomersView,
                Permissions.LoansView,
                Permissions.LoansManage,
                Permissions.ReportsView,
            ]),
        new(
            RoleCodes.Cashier,
            "Cajero",
            [
                Permissions.CustomersView,
                Permissions.LoansView,
                Permissions.PaymentsManage,
                Permissions.CashManage,
            ]),
        new(
            RoleCodes.Collector,
            "Cobrador",
            [
                Permissions.CustomersView,
                Permissions.LoansView,
                Permissions.CollectionsManage,
            ]),
        new(
            RoleCodes.Auditor,
            "Auditor / Contador",
            ReadOnly),
    ];

    public static RoleDefinition Find(string code) =>
        All.FirstOrDefault(role => string.Equals(role.Code, code, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Unknown system role '{code}'.");

    public static bool IsKnown(string code) =>
        All.Any(role => string.Equals(role.Code, code, StringComparison.Ordinal));
}

public sealed record RoleDefinition(
    string Code,
    string Name,
    IReadOnlyList<string> Permissions);
