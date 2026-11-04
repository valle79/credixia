-- Script para eliminar tablas duplicadas (PascalCase)
-- Solo mantener las tablas correctas (snake_case)
-- ADVERTENCIA: Ejecutar solo si estás seguro de que estas tablas están vacías

-- Verificar primero que las tablas PascalCase estén vacías
DO $$
DECLARE
    r RECORD;
    v_count INTEGER;
BEGIN
    FOR r IN 
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name IN ('Clients', 'CollectionActions', 'Installments', 'Loans', 
                          'PaymentAllocations', 'Payments', 'Tenants', 'Users')
    LOOP
        EXECUTE format('SELECT COUNT(*) FROM %I', r.table_name) INTO v_count;
        IF v_count > 0 THEN
            RAISE NOTICE 'ADVERTENCIA: La tabla % tiene % registros', r.table_name, v_count;
        ELSE
            RAISE NOTICE 'La tabla % está vacía y puede ser eliminada', r.table_name;
        END IF;
    END LOOP;
END $$;

-- Eliminar las tablas duplicadas (solo si están vacías)
-- Descomenta las siguientes líneas SOLO después de verificar que están vacías:

-- DROP TABLE IF EXISTS "Clients" CASCADE;
-- DROP TABLE IF EXISTS "CollectionActions" CASCADE;
-- DROP TABLE IF EXISTS "Installments" CASCADE;
-- DROP TABLE IF EXISTS "Loans" CASCADE;
-- DROP TABLE IF EXISTS "PaymentAllocations" CASCADE;
-- DROP TABLE IF EXISTS "Payments" CASCADE;
-- DROP TABLE IF EXISTS "Tenants" CASCADE;
-- DROP TABLE IF EXISTS "Users" CASCADE;

-- Verificar tablas restantes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;
