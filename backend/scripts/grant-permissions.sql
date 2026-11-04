-- Script para otorgar permisos completos al usuario administrador del tenant
-- Ejecutar este script después de crear el tenant y el usuario

-- 1. Obtener el ID del tenant (reemplaza 'mi-empresa' con tu slug de tenant)
-- SELECT id FROM tenants WHERE slug = 'mi-empresa';

-- 2. Obtener el ID del rol de administrador del tenant
-- SELECT id FROM roles WHERE code = 'tenant_admin' AND tenant_id = '<TENANT_ID>';

-- 3. Obtener el ID de la membresía del usuario
-- SELECT id FROM tenant_memberships WHERE user_id = '<USER_ID>' AND tenant_id = '<TENANT_ID>';

-- 4. Asignar el rol de administrador a la membresía
-- Reemplaza <MEMBERSHIP_ID> y <ROLE_ID> con los valores obtenidos
-- INSERT INTO membership_roles (membership_id, role_id)
-- VALUES ('<MEMBERSHIP_ID>', '<ROLE_ID>')
-- ON CONFLICT DO NOTHING;

-- ALTERNATIVA: Otorgar TODOS los permisos al usuario del tenant
-- (Útil para desarrollo, ajustar en producción)

DO $$
DECLARE
    v_tenant_id UUID;
    v_user_id UUID;
    v_membership_id UUID;
    v_admin_role_id UUID;
BEGIN
    -- Buscar el primer tenant activo
    SELECT id INTO v_tenant_id
    FROM tenants
    WHERE status = 'Active'
    ORDER BY created_at
    LIMIT 1;

    IF v_tenant_id IS NULL THEN
        RAISE EXCEPTION 'No se encontró ningún tenant activo';
    END IF;

    -- Buscar el primer usuario que sea miembro de este tenant
    SELECT user_id, id INTO v_user_id, v_membership_id
    FROM tenant_memberships
    WHERE tenant_id = v_tenant_id
      AND status = 'Active'
    ORDER BY created_at
    LIMIT 1;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'No se encontró ningún usuario activo en el tenant %', v_tenant_id;
    END IF;

    -- Buscar o crear el rol de administrador del tenant
    SELECT id INTO v_admin_role_id
    FROM roles
    WHERE tenant_id = v_tenant_id
      AND code = 'tenant_admin';

    IF v_admin_role_id IS NULL THEN
        RAISE EXCEPTION 'No se encontró el rol tenant_admin para el tenant %', v_tenant_id;
    END IF;

    -- Asignar el rol de administrador a la membresía
    INSERT INTO membership_roles (membership_id, role_id)
    VALUES (v_membership_id, v_admin_role_id)
    ON CONFLICT DO NOTHING;

    -- Verificar que el rol tenga todos los permisos
    -- (El rol debería haberse creado automáticamente con permisos)
    
    RAISE NOTICE 'Permisos asignados correctamente';
    RAISE NOTICE 'Tenant ID: %', v_tenant_id;
    RAISE NOTICE 'User ID: %', v_user_id;
    RAISE NOTICE 'Membership ID: %', v_membership_id;
    RAISE NOTICE 'Role ID: %', v_admin_role_id;
END $$;
