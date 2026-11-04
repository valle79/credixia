# Lending Portal

Plataforma SaaS multitenant para la gestión de préstamos, créditos y cobranza en Perú.
El producto se entrega por fases; este repositorio contiene la **fase 1: base técnica**.

## Estado actual

| Componente | Estado |
| --- | --- |
| Solución .NET 10 por capas | Listo |
| Dominio inicial (dinero, tenants) | Listo |
| Persistencia EF Core + PostgreSQL | Listo |
| API con health checks, ProblemDetails y observabilidad | Listo |
| Portal React 19 + TypeScript estricto | Listo |
| Pruebas unitarias, arquitectura e integración | Listo (50 pruebas backend, 4 frontend) |
| Docker y pipeline de CI | Listo |
| Identidad, RLS activo y módulos de negocio | Fase 2 en adelante |

La especificación funcional completa vive en [`docs/fase-0/`](docs/fase-0/).
Las decisiones de arquitectura están en [`docs/adr/`](docs/adr/).

## Stack

- **Backend:** .NET 10, ASP.NET Core Minimal APIs, EF Core 10, Npgsql, Serilog, OpenTelemetry.
- **Base de datos:** PostgreSQL 17 (Neon en despliegue; contenedor en local).
- **Frontend:** React 19, TypeScript estricto, Vite 8, Tailwind CSS 4, React Router 7, TanStack Query 5.
- **Pruebas:** xUnit, NetArchTest, Testcontainers, Vitest, Testing Library.
- **Infraestructura:** Docker Compose, nginx, GitHub Actions.

## Estructura

```text
backend/
  src/
    Lending.Domain/          Entidades y reglas sin dependencias de infraestructura
    Lending.Application/     Casos de uso, resultados y abstracciones
    Lending.Infrastructure/  EF Core, PostgreSQL, salud y reloj
    Lending.Api/             Host HTTP, middleware, endpoints y observabilidad
  tests/
    Lending.UnitTests/
    Lending.IntegrationTests/
    Lending.ArchitectureTests/
frontend/                    Portal web (Vite + React)
docs/
  fase-0/                    Requisitos, arquitectura y decisiones aprobadas
  adr/                       Registros de decisiones de arquitectura
```

## Requisitos

- .NET SDK **10.0.100** o superior dentro del canal 10 (`global.json`).
- Node.js **20.19+** (se recomienda 22 LTS o superior).
- Docker Desktop con Docker Compose v2 (para PostgreSQL local e integración).

## Inicio rápido (desarrollo local)

1. Crear el archivo de entorno local:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Levantar PostgreSQL:

   ```powershell
   docker compose up -d postgres
   ```

3. Aplicar migraciones (PowerShell):

   ```powershell
   $env:ConnectionStrings__Postgres = "Host=localhost;Port=55432;Database=lending;Username=lending;Password=lending_dev_only"
   dotnet tool restore
   dotnet tool run dotnet-ef database update --project backend/src/Lending.Infrastructure --startup-project backend/src/Lending.Api
   ```

4. Ejecutar la API (queda en `http://localhost:5111`, Swagger/Scalar en `/scalar`):

   ```powershell
   dotnet run --project backend/src/Lending.Api
   ```

5. Ejecutar el portal:

   ```powershell
   Set-Location frontend
   npm install
   npm run dev
   ```

   El portal queda en `http://localhost:5173` y redirige `/api`, `/health`, `/openapi` y `/scalar` a la API.

## Docker Compose completo

```powershell
Copy-Item .env.example .env
docker compose up --build
```

- Portal: `http://localhost:3000`
- API: `http://localhost:5111`
- PostgreSQL: `localhost:55432`

Las migraciones **no se ejecutan automáticamente** en los contenedores. Aplícalas con el
comando del paso 3 antes de usar el portal. El contenedor del portal usa nginx y redirige
`/api` hacia el servicio `api`, por lo que no requiere CORS.

## Pruebas y verificación

```powershell
dotnet build Lending.slnx -c Debug
dotnet test Lending.slnx -c Debug
dotnet format Lending.slnx --verify-no-changes

Set-Location frontend
npm run lint
npm run test
npm run build
```

Las pruebas de integración usan Testcontainers: requieren Docker en ejecución. Levantan
`postgres:17-alpine`, aplican las migraciones y validan la API y la persistencia real.

## Configuración

| Clave | Uso | Valor local |
| --- | --- | --- |
| `ConnectionStrings__Postgres` | Conexión Npgsql | `Host=localhost;Port=55432;...` |
| `Cors__AllowedOrigins__0` | Origen permitido en desarrollo | `http://localhost:5173` |
| `Api__ExposeOpenApi` | Expone OpenAPI y Scalar fuera de Development | `false` |
| `OpenTelemetry__OtlpEndpoint` | Exportador OTLP opcional | vacío |
| `POSTGRES_*` | Credenciales del contenedor local | ver `.env.example` |
| `API_PORT` / `WEB_PORT` | Puertos publicados por Compose | `5111` / `3000` |

No se versionan secretos. La contraseña `lending_dev_only` es exclusiva para el entorno
local y está documentada como tal.

## Seguridad y multitenancy

- Todas las consultas de negocio se aislarán por `tenant_id` con RLS de PostgreSQL
  (`FORCE ROW LEVEL SECURITY`, rol de aplicación no propietario). El tenant se resuelve
  desde la sesión del usuario, nunca desde datos enviados por el cliente.
- La autenticación (access token en memoria, refresh rotativo en cookie `httpOnly`) se
  implementa en la fase 2.
- La API expone endpoints de salud y un `ping`; no hay datos de negocio todavía.

## Documentación

- [Resumen y alcance](docs/fase-0/00-resumen-alcance.md)
- [Requisitos](docs/fase-0/01-requisitos.md)
- [Arquitectura y dominio](docs/fase-0/02-arquitectura-dominio.md)
- [Multitenancy y seguridad](docs/fase-0/03-multitenancy-seguridad.md)
- [Motor financiero](docs/fase-0/04-motor-financiero.md)
- [Plan, riesgos y decisiones](docs/fase-0/05-plan-riesgos-decisiones.md)

## Decisiones pendientes antes de la fase 2

Las decisiones `DB-01` a `DB-05` deben confirmarse (y `DB-05` requiere validación legal:
topes de tasa y comisiones) antes de implementar identidad, RLS y el motor financiero.
