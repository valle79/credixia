-- =====================================================
-- Script para aceptar invitación manualmente
-- Usar cuando no tienes email configurado
-- =====================================================

-- PASO 1: Ver las invitaciones pendientes
SELECT 
    i.id,
    i.email,
    t.name as tenant_name,
    t.slug as tenant_slug,
    i.created_at,
    i.expires_at
FROM invitations i
JOIN tenants t ON i.tenant_id = t.id
WHERE i.accepted_at IS NULL
ORDER BY i.created_at DESC;

-- PASO 2: Obtener el token en texto plano de la última invitación
-- NOTA: Este script solo funciona si guardaste el token antes de hashearlo
-- El sistema guarda el hash, no el token original
-- Por lo tanto, necesitas usar el endpoint de la API con el token que obtuviste

-- ALTERNATIVA: Crear usuario directamente como OWNER del tenant

DO $$
DECLARE
    v_tenant_id UUID;
    v_invitation_id UUID;
    v_user_id UUID;
    v_membership_id UUID;
    v_owner_role_id UUID;
    v_email TEXT := 'gerente@miempresa.com';  -- CAMBIAR ESTE EMAIL
    v_password_hash TEXT;
    v_now TIMESTAMPTZ := NOW();
BEGIN
    -- Hashear password "Admin123456!" 
    -- NOTA: Debes usar el mismo password hasher que la aplicación
    -- Este es un hash de ejemplo, NO ES VÁLIDO
    v_password_hash := '$2a$11$ejemplo_hash_no_valido';
    
    -- Buscar el tenant recién creado
    SELECT id INTO v_tenant_id
    FROM tenants
    WHERE slug = 'mi-empresa'  -- CAMBIAR ESTE SLUG
    LIMIT 1;

    IF v_tenant_id IS NULL THEN
        RAISE EXCEPTION 'Tenant no encontrado';
    END IF;

    -- Buscar la invitación
    SELECT id INTO v_invitation_id
    FROM invitations
    WHERE tenant_id = v_tenant_id
      AND email = v_email
      AND accepted_at IS NULL
    LIMIT 1;

    IF v_invitation_id IS NULL THEN
        RAISE EXCEPTION 'Invitación no encontrada';
    END IF;

    -- IMPORTANTE: En lugar de este script SQL complejo,
    -- es mejor usar el endpoint de la API /api/v1/auth/accept-invitation
    -- con el token que obtuviste al crear el tenant

    RAISE NOTICE 'Tenant ID: %', v_tenant_id;
    RAISE NOTICE 'Invitation ID: %', v_invitation_id;
    RAISE NOTICE '';
    RAISE NOTICE '====================================================';
    RAISE NOTICE 'RECOMENDACIÓN:';
    RAISE NOTICE 'Usa el endpoint de la API para aceptar la invitación:';
    RAISE NOTICE 'POST /api/v1/auth/accept-invitation';
    RAISE NOTICE 'Body: { "email": "gerente@miempresa.com", "password": "TuPassword123!", "token": "TOKEN_DE_INVITACION" }';
    RAISE NOTICE '====================================================';
END $$;
