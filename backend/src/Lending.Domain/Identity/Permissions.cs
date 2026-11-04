namespace Lending.Domain.Identity;

/// <summary>
/// Permission catalog for phase 2. Future modules add codes here and map them to roles;
/// clients never send permissions.
/// </summary>
public static class Permissions
{
    public const string PlatformTenantsManage = "platform.tenants.manage";

    public const string MembersView = "members.view";
    public const string MembersManage = "members.manage";
    public const string RolesView = "roles.view";
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";
    public const string AuditView = "audit.view";

    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";
    public const string LoansView = "loans.view";
    public const string LoansManage = "loans.manage";
    public const string PaymentsManage = "payments.manage";
    public const string CashManage = "cash.manage";
    public const string CollectionsManage = "collections.manage";
    public const string ReportsView = "reports.view";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(PlatformTenantsManage, "Crear y administrar organizaciones de la plataforma."),
        new(MembersView, "Consultar usuarios y membresías del tenant."),
        new(MembersManage, "Invitar usuarios y administrar membresías."),
        new(RolesView, "Consultar roles y permisos."),
        new(SettingsView, "Consultar la configuración del tenant."),
        new(SettingsManage, "Modificar la configuración del tenant."),
        new(AuditView, "Consultar eventos de auditoría."),
        new(CustomersView, "Consultar clientes."),
        new(CustomersManage, "Crear y modificar clientes."),
        new(LoansView, "Consultar préstamos y cronogramas."),
        new(LoansManage, "Crear y modificar préstamos."),
        new(PaymentsManage, "Registrar pagos y reversos."),
        new(CashManage, "Operar caja y desembolsos."),
        new(CollectionsManage, "Gestionar cobranza y promesas."),
        new(ReportsView, "Consultar y exportar reportes."),
    ];
}

public sealed record PermissionDefinition(string Code, string Description);
