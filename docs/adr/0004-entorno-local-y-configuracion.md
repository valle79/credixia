# ADR 0004: Entorno local y configuración

- Estado: aceptado
- Fecha: 2026-10-29

## Contexto

El equipo necesita un entorno reproducible con PostgreSQL, API y portal sin secretos
versionados ni puertos en conflicto con otros proyectos locales.

## Decisión

- PostgreSQL local se publica en el puerto **55432** mediante Docker Compose.
- La API de desarrollo escucha en **5111**; el portal Vite en **5173** y el portal en
  contenedor en **3000**.
- La contraseña `lending_dev_only` es un valor de desarrollo documentado; cualquier
  entorno compartido debe usar variables de entorno o un gestor de secretos.
- Las migraciones se aplican explícitamente con `dotnet ef`; la API no migra al iniciar.
- La configuración se resuelve por variables de entorno con doble guion bajo
  (`ConnectionStrings__Postgres`), compatible con contenedores y CI.
- El frontend en contenedor sirve la SPA con nginx y redirige `/api` y `/health` a la API,
  evitando CORS en ese escenario.

## Consecuencias

- El arranque local requiere dos pasos (contenedor y migración) y es explícito.
- No hay credenciales reales en el repositorio.
- Los puertos elegidos no chocan con los servicios habituales del equipo.
