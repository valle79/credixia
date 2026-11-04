-- ========================================
-- Script para eliminar tablas duplicadas
-- Base de datos: Neon PostgreSQL
-- ========================================

-- PASO 1: Ver todas las tablas y sus registros
SELECT 
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as num_columnas
FROM information_schema.tables t
WHERE table_schema = 'public'
ORDER BY table_name;

-- PASO 2: Eliminar tablas duplicadas con nombres en PascalCase (están vacías)
-- Estas son las tablas viejas que no se usan

DROP TABLE IF EXISTS "Clients" CASCADE;
DROP TABLE IF EXISTS "CollectionActions" CASCADE;
DROP TABLE IF EXISTS "Installments" CASCADE;
DROP TABLE IF EXISTS "Loans" CASCADE;
DROP TABLE IF EXISTS "PaymentAllocations" CASCADE;
DROP TABLE IF EXISTS "Payments" CASCADE;
DROP TABLE IF EXISTS "Tenants" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;

-- PASO 3: Verificar que solo queden las tablas correctas (snake_case)
SELECT 
    table_name,
    pg_size_pretty(pg_total_relation_size('public.' || table_name)) AS tamaño
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- RESULTADO ESPERADO: Solo deben quedar tablas como:
-- audit_events, cash_sessions, customers, loans, payments, tenants, users, etc.
-- (todas en minúsculas con guiones bajos)
