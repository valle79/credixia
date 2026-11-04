# Fase 0 — Descubrimiento y especificación

**Proyecto:** Plataforma SaaS multitenant de préstamos, créditos y cobranzas
**Mercado inicial:** Perú
**Fecha:** 2026-10-28
**Estado:** Borrador para aprobación — **no habilita implementación todavía**

Esta carpeta contiene exclusivamente los entregables de la Fase 0. No se ha creado
código de aplicación, solución .NET, proyecto React ni migraciones.

## Documentos

| Archivo | Contenido |
|---|---|
| [00-resumen-alcance.md](00-resumen-alcance.md) | Resumen del producto, alcance, actores, roles, flujos, supuestos y glosario |
| [01-requisitos.md](01-requisitos.md) | Requisitos funcionales, no funcionales y matriz de aceptación |
| [02-arquitectura-dominio.md](02-arquitectura-dominio.md) | Arquitectura, módulos, modelo de dominio, estados, modelo de datos, API y frontend |
| [03-multitenancy-seguridad.md](03-multitenancy-seguridad.md) | Aislamiento multitenant, modelo de amenazas, OWASP, privacidad y cumplimiento en Perú |
| [04-motor-financiero.md](04-motor-financiero.md) | Reglas financieras, convenciones, redondeo, mora y casos de prueba de referencia |
| [05-plan-riesgos-decisiones.md](05-plan-riesgos-decisiones.md) | Plan por fases, dependencias, riesgos y decisiones bloqueantes |

## Regla de avance

La Fase 1 no comienza hasta que:

1. Se apruebe este documento en conjunto.
2. Se resuelvan las decisiones marcadas como **bloqueantes** en
   [05-plan-riesgos-decisiones.md](05-plan-riesgos-decisiones.md) (sección 6).
3. Se confirme la versión objetivo del stack (ver `02-arquitectura-dominio.md`).

## Convenciones de este repositorio

- Documentación funcional y de negocio: español.
- Código, nombres de tablas, columnas, endpoints y ramas: inglés.
- Importes monetarios: `decimal` en C# / `numeric` en PostgreSQL. Nunca `float`/`double`.
- Fechas de negocio: `date` / `DateOnly`. Marcas de tiempo: `timestamptz` UTC.
- Zona horaria de negocio por defecto: `America/Lima`.
- Todo secreto va en variables de entorno o gestor de secretos. Nunca en el repositorio.
