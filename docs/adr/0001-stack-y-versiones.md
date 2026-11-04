# ADR 0001: Stack y versiones de la fase 1

- Estado: aceptado
- Fecha: 2026-10-29

## Contexto

La plataforma necesita una base técnica mantenible para un SaaS financiero multitenant,
con despliegue en contenedores, pruebas automatizadas y un portal web tipado.

## Decisión

- Backend en **.NET 10** con Minimal APIs y arquitectura por capas
  (`Domain`, `Application`, `Infrastructure`, `Api`).
- Persistencia con **EF Core 10** y **Npgsql 10** sobre **PostgreSQL 17**.
- Pruebas backend con **xUnit**, **NetArchTest** (reglas de dependencia) y
  **Testcontainers** para integración real con PostgreSQL.
- Frontend con **React 19**, **Vite 8**, **TypeScript estricto**, **Tailwind CSS 4**,
  **React Router 7** y **TanStack Query 5**.
- Pruebas frontend con **Vitest 4** y **Testing Library** sobre **jsdom 27**.
- Versiones centralizadas en `Directory.Packages.props` y `package.json` (sin rangos
  flotantes).
- TypeScript se fija en **6.0.3** porque `typescript-eslint` 8.x todavía declara soporte
  `<6.1.0`. Se reevaluará cuando el ecosistema adopte TypeScript 7.

## Consecuencias

- Un solo SDK y un solo runtime por servicio.
- Las reglas de dependencia se verifican automáticamente en CI.
- Las pruebas de integración requieren Docker, igual que el entorno local.
- La actualización de TypeScript a 7 queda bloqueada hasta que el linter la soporte.
