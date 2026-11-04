using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lending.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2RowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY roles_tenant ON roles FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE role_permissions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE role_permissions FORCE ROW LEVEL SECURITY;
                CREATE POLICY role_permissions_tenant ON role_permissions FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM roles
                            WHERE roles.id = role_permissions.role_id
                            AND roles.tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM roles
                            WHERE roles.id = role_permissions.role_id
                            AND roles.tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );

                ALTER TABLE tenant_memberships ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_memberships FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_memberships_tenant ON tenant_memberships FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );
                CREATE POLICY tenant_memberships_self ON tenant_memberships FOR SELECT
                    USING (
                        user_id = NULLIF(current_setting('app.user_id', true), '')::uuid
                    );

                ALTER TABLE membership_roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE membership_roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY membership_roles_tenant ON membership_roles FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE tenant_invitations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_invitations FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_invitations_tenant ON tenant_invitations FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE tenant_invitation_roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_invitation_roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_invitation_roles_tenant ON tenant_invitation_roles FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM tenant_invitations
                            WHERE tenant_invitations.id = tenant_invitation_roles.invitation_id
                            AND tenant_invitations.tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR EXISTS (
                            SELECT 1 FROM tenant_invitations
                            WHERE tenant_invitations.id = tenant_invitation_roles.invitation_id
                            AND tenant_invitations.tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                        )
                    );

                ALTER TABLE tenant_settings ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_settings FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_settings_tenant ON tenant_settings FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );

                ALTER TABLE audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE audit_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY audit_events_tenant ON audit_events FOR ALL
                    USING (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    )
                    WITH CHECK (
                        current_setting('app.is_platform_admin', true) = 'true'
                        OR tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid
                    );
                CREATE POLICY audit_events_self_insert ON audit_events FOR INSERT
                    WITH CHECK (
                        tenant_id IS NULL
                        AND actor_user_id = NULLIF(current_setting('app.user_id', true), '')::uuid
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS audit_events_self_insert ON audit_events;
                DROP POLICY IF EXISTS audit_events_tenant ON audit_events;
                ALTER TABLE audit_events NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE audit_events DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_settings_tenant ON tenant_settings;
                ALTER TABLE tenant_settings NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE tenant_settings DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_invitation_roles_tenant ON tenant_invitation_roles;
                ALTER TABLE tenant_invitation_roles NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE tenant_invitation_roles DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_invitations_tenant ON tenant_invitations;
                ALTER TABLE tenant_invitations NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE tenant_invitations DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS membership_roles_tenant ON membership_roles;
                ALTER TABLE membership_roles NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE membership_roles DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_memberships_self ON tenant_memberships;
                DROP POLICY IF EXISTS tenant_memberships_tenant ON tenant_memberships;
                ALTER TABLE tenant_memberships NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE tenant_memberships DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS role_permissions_tenant ON role_permissions;
                ALTER TABLE role_permissions NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE role_permissions DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS roles_tenant ON roles;
                ALTER TABLE roles NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE roles DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}
