# 05 — Plan por fases, riesgos y decisiones pendientes

## 1. Plan de implementación

Cada fase termina con criterios de salida verificables. No se avanza si una fase deja
fallos conocidos en seguridad, aislamiento o cálculos.

| Fase | Objetivo | Entregables | Criterio de salida | Depende de |
|---|---|---|---|---|
| 0 | Descubrimiento | Este paquete documental | Aprobación + decisiones bloqueantes resueltas | — |
| 1 | Base técnica | Solución .NET, React, Docker Compose, PostgreSQL local, CI, ADRs, versiones fijadas | `build` + `test` verdes; app levanta local; health checks | Fase 0 |
| 2 | Identidad y multitenancy | Tenants, membresías, roles/permisos, JWT+refresh, contexto de tenant, RLS, auditoría | Suite de aislamiento verde; RF-TEN-01..03, RF-AUTH-01..10 | Fase 1 |
| 3 | Clientes y solicitudes | CRUD, búsqueda, duplicados, importación, solicitudes y estados | RF-CUS y RF-APP con pruebas; aislamiento verificado | Fase 2 |
| 4 | Motor financiero | Contratos, cálculos, cronogramas, redondeo, simulación, pruebas de referencia | Casos C-01..C-10 y matriz 1..20 en verde; revisión financiera | Fase 1 |
| 5 | Préstamos, desembolsos y pagos | Aprobación, desembolso, pagos, aplicaciones, idempotencia, concurrencia, reversos | RF-LOAN, RF-PAY, RF-CASH-03; pruebas de concurrencia | Fases 3, 4 |
| 6 | Caja y cobranza | Sesiones, movimientos, cierre, cartera, gestiones, promesas, reprogramación | RF-CASH, RF-COL, RF-LOAN-06 | Fase 5 |
| 7 | Documentos y notificaciones | PDF, storage privado, URLs firmadas, WhatsApp, outbox, webhooks | RF-DOC, RF-NOT; pruebas de webhook y reintentos | Fases 2, 5 |
| 8 | Dashboard y reportes | Indicadores, reportes, exportaciones, jobs | RF-DASH, RF-REP; totales contrastados | Fases 5, 6 |
| 9 | Endurecimiento | Seguridad, rendimiento, restauración, límites de plan, break-glass | Checklist de producción + pentest interno + restore probado | Fases 1–8 |
| 10 | Despliegue | Staging, aceptación, producción, manual de operación | Criterios de la sección 5 cumplidos | Fase 9 |

### 1.1 Orden recomendado dentro de la Fase 1

1. `global.json`, solución y proyectos; `Directory.Build.props` con analyzers.
2. Frontend Vite + TS estricto + lint + Vitest.
3. Docker Compose con PostgreSQL local y health checks.
4. EF Core + migración inicial vacía + `dotnet ef` reproducible.
5. Serilog/OpenTelemetry + Problem Details + OpenAPI.
6. CI (build, test, lint, format, dependency scan, architecture tests).
7. ADRs de versiones y de sesión (cookie vs. memoria).
8. `README` de arranque local y `.env.example`.

## 2. Dependencias externas y accesos necesarios

| Dependencia | Necesaria para | Estado | Nota |
|---|---|---|---|
| Proyecto Neon y credenciales | Fase 1 (dev), Fase 9 (staging/prod) | **Pendiente** | Usar ramas de Neon para entornos |
| Repositorio Git remoto y CI | Fase 1 | **Pendiente** | Asumido GitHub Actions; confirmar |
| Destino de backend | Fase 1/10 | **Pendiente** | Neon no aloja API/frontend |
| Destino de frontend estático | Fase 1/10 | **Pendiente** | Vercel/Cloudflare/Azure SWA/etc. |
| Object storage privado | Fase 7 | **Pendiente** | S3/R2/Azure Blob + URLs firmadas |
| Proveedor de correo | Fase 2 | **Pendiente** | Resend/SES/etc. |
| WhatsApp Business Platform | Fase 7 | **Pendiente** | Decidir WABA por tenant o compartida |
| Proveedor de observabilidad | Fase 9 | **Pendiente** | Grafana Cloud/Sentry/App Insights |
| Asesoría legal | Antes de producción | **Pendiente** | Tasas, contratos, datos personales, cobranza |
| Antivirus/scan de adjuntos | Fase 7 | **Pendiente** | Servicio o librería con actualización |

Sin estos accesos se puede avanzar en Fases 1–3 con PostgreSQL local y adaptadores
simulados, pero **no** se puede declarar producción.

## 3. Riesgos

Escala: probabilidad e impacto `B` (bajo), `M` (medio), `A` (alto), `C` (crítico).

| ID | Riesgo | Prob. | Impacto | Mitigación | Fase |
|---|---|---|---|---|---|
| R-01 | Fuga de datos entre tenants | M | C | RLS + FK compuestas + suite de aislamiento + revisión | 2, 9 |
| R-02 | Cálculo financiero incorrecto | M | C | Casos de referencia, revisión dual, pruebas de propiedad, reconciliación | 4 |
| R-03 | Pago duplicado o sobrepago | A | C | Idempotencia, UNIQUE, bloqueo, pruebas concurrentes | 5 |
| R-04 | Reglas legales mal parametrizadas | A | C | Límites de plataforma, validación jurídica, snapshot por operación | 4, 9 |
| R-05 | Pérdida de datos | B | C | PITR de Neon + dumps + restore probado | 9 |
| R-06 | Migración defectuosa en producción | M | A | Migraciones compatibles, backup, staging, rollback/forward-fix | 9 |
| R-07 | Fuga de PII en logs/PDF | M | A | Scrubbing, enlaces firmados, revisión de muestras | 7, 9 |
| R-08 | WhatsApp bloqueado por uso indebido | M | A | Plantillas aprobadas, consentimiento, límites, opt-out | 7 |
| R-09 | Baja adopción por UX compleja | M | A | Flujo de cobro en ≤ 3 pasos, pruebas con usuarios, móvil primero | 5, 9 |
| R-10 | Sobrecarga de conexiones a Neon | M | M | Pooling, límites, timeouts, consultas indexadas, job throttling | 1, 9 |
| R-11 | Acceso excesivo del superadmin | B | A | Break-glass auditado, sin acceso por defecto | 9 |
| R-12 | Dependencia vulnerable | M | A | Escaneo en CI, lockfiles, SBOM, política de parches | 1, 9 |
| R-13 | Coste operativo del storage/WhatsApp | M | M | Límites por plan, métricas de consumo, retención | 9 |
| R-14 | Alcance creciente (“scope creep”) | A | M | Fases cerradas, matriz de requisitos, cambios por ADR | Todas |
| R-15 | Falta de asesoría legal antes de lanzar | A | C | Bloqueo explícito de producción hasta validación | 9 |

## 4. Matriz de riesgos legales pendientes (resumen)

| Tema | Riesgo si no se valida | Acción |
|---|---|---|
| Tasas máximas | Cobro de intereses ilegales, nulidad, sanciones | Fijar topes por tipo de tenant/producto con fuente oficial |
| Capitalización de intereses | Práctica prohibida en ciertos créditos | Bandera por producto + validación legal |
| Contratos y transparencia | Reclamos y sanciones | Plantillas revisadas; PDF con condiciones completas |
| Datos personales | Sanciones de ANPD | Registro de bancos, consentimientos, DPA, retención |
| Cobranza | Denuncias por prácticas abusivas | Límites de frecuencia/horario, guiones, opt-out |
| Tributación | Contingencia fiscal | Definir comprobantes/IGV con contador |
| Condición del prestamista | Operación no autorizada | Validar si requiere autorización específica |

## 5. Criterios para declarar “listo para producción”

Ninguno de estos puntos puede quedar en “pendiente” al momento de habilitar clientes reales:

- [ ] Aislamiento entre tenants demostrado por suite automatizada en CI.
- [ ] Autorización por recurso/operación/tenant probada con casos negativos.
- [ ] Motor financiero validado con casos de referencia y revisión independiente.
- [ ] Idempotencia y concurrencia de pagos probadas bajo carga.
- [ ] Reversos y reconciliación de saldos y caja verificados.
- [ ] Migraciones y restauración de respaldo probadas en staging.
- [ ] Secretos en gestor; escaneo de dependencias sin críticos abiertos.
- [ ] Rate limiting, CORS, CSP, HTTPS y headers verificados.
- [ ] Observabilidad, alertas y runbooks operativos.
- [ ] Documentos privados con URLs firmadas y autorización por tenant.
- [ ] WhatsApp con firma de webhook, consentimiento y opt-out.
- [ ] Validación legal de tasas, contratos, datos y cobranza.
- [ ] Pruebas E2E de los 15 flujos del alcance en verde.
- [ ] Aceptación formal del negocio y manual de operación entregado.

## 6. Decisiones bloqueantes (requieren tu confirmación antes de Fase 1/4/5)

| ID | Decisión | Por qué bloquea | Propuesta por defecto |
|---|---|---|---|
| DB-01 | Métodos de interés v1 | Define dominio, contratos y cronogramas | Flat + Francés + Alemán, interés simple |
| DB-02 | Convención de tasa y días | Cambia todos los importes | Tasa por período explícita; nominal anual lineal; Actual/365 |
| DB-03 | Política de redondeo | Afecta reconciliación y PDF | HALF_UP, 2 decimales, residuo en última cuota |
| DB-04 | Orden de imputación y base de mora | Afecta saldos y cobranza | Comisiones → mora → interés → capital; mora sobre capital vencido |
| DB-05 | Límites legales de tasas | Bloquea operar con dinero real | Tabla de topes por tipo de prestamista, validada por abogado |
| DB-06 | Moneda v1 | Afecta modelo y UI | PEN únicamente, modelo preparado para más |
| DB-07 | WhatsApp: WABA por tenant o compartida | Afecta costes, onboarding, cumplimiento | WABA por tenant cuando el volumen lo justifique |
| DB-08 | Acceso del superadmin | Afecta privacidad y confianza | Sin acceso por defecto; break-glass aprobado y auditado |
| DB-09 | Destino de despliegue y proveedores | Afecta CI/CD, secretos y costes | Azure Container Apps + Cloudflare Pages + R2 (a confirmar) |
| DB-10 | Mecanismo de sesión frontend | Afecta CSRF/XSS | Access en memoria + refresh httpOnly rotativo |
| DB-11 | Retención y eliminación de datos | Afecta cumplimiento y diseño | Retención legal mínima; inactivación en lugar de borrado |
| DB-12 | Alcance de la importación inicial | Afecta esfuerzo de Fase 3 | CSV/Excel con previsualización y reporte por fila |

Las decisiones DB-01 a DB-05 son **imprescindibles** antes de la Fase 4. DB-07, DB-09 y
DB-11 son imprescindibles antes de las Fases 7, 1 y 9 respectivamente.

## 7. Estado de la entrega Fase 0

| Entregable | Estado |
|---|---|
| 1. Resumen del producto | Entregado |
| 2. Actores y roles | Entregado |
| 3. Requisitos funcionales y no funcionales | Entregado |
| 4. Flujos principales | Entregado |
| 5. Reglas financieras por definir | Entregado |
| 6. Arquitectura propuesta | Entregado |
| 7. Estrategia multitenant | Entregado |
| 8. Modelo de dominio inicial | Entregado |
| 9. Riesgos de seguridad y privacidad | Entregado |
| 10. Requisitos legales a validar | Entregado |
| 11. Matriz de requisitos y aceptación | Entregado |
| 12. Plan de fases y dependencias | Entregado |
| 13. Decisiones pendientes que bloquean | Entregado (sección 6) |

## 8. Próximo paso

Con tu aprobación y las respuestas a DB-01..DB-05, DB-09 y DB-10, la Fase 1 entregará:

- Solución .NET 10 con proyectos y reglas de dependencia.
- Frontend React + TypeScript estricto con lint, formato y prueba base.
- Docker Compose local con PostgreSQL.
- EF Core configurado y migración inicial.
- CI con build, tests, lint, formato, escaneo de dependencias y architecture tests.
- ADRs de versiones, sesión y entornos.
- `README` de arranque local y `.env.example` sin secretos.

No se escribirá lógica financiera ni se crearán tablas de negocio hasta resolver DB-01..DB-05.
