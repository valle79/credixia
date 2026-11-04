-- Script para verificar y corregir el estado de los tenants
-- Ejecutar este script si tienes problemas de "organización suspendida"

-- Ver el estado actual de todos los tenants
SELECT id, slug, name, status, created_at, updated_at
FROM tenants
ORDER BY created_at DESC;

-- Reactivar todos los tenants suspendidos
UPDATE tenants
SET status = 'Active',
    updated_at = NOW()
WHERE status = 'Suspended';

-- Verificar los cambios
SELECT id, slug, name, status, created_at, updated_at
FROM tenants
ORDER BY created_at DESC;
