# ADR 0002: Multitenancy compartido con RLS

- Estado: aceptado (implementación en fase 2)
- Fecha: 2026-10-29

## Contexto

Cada entidad financiera (tenant) debe operar con datos completamente aislados sin
multiplicar esquemas ni bases de datos por cliente. El costo operativo y las migraciones
deben seguir siendo manejables.

## Decisión

- Esquema compartido con columna `tenant_id` en todas las tablas de negocio.
- Claves foráneas compuestas que incluyen `tenant_id` para impedir referencias cruzadas.
- **Row Level Security** de PostgreSQL habilitado con `FORCE ROW LEVEL SECURITY`.
- El rol de la aplicación no será propietario de las tablas; la sesión define
  `SET LOCAL app.tenant_id` dentro de la transacción.
- Política fail-closed: sin tenant válido no se devuelve ninguna fila.
- El tenant se resuelve exclusivamente desde la sesión autenticada y la membresía del
  usuario; nunca desde un encabezado, query string o cuerpo enviado por el cliente.

## Consecuencias

- Una sola migración para todos los tenants.
- Las pruebas deben cubrir intentos de acceso cruzado y ausencia de contexto.
- Las tareas administrativas requieren un rol separado o un mecanismo de excepción
  auditado (break-glass), nunca el rol de aplicación.
