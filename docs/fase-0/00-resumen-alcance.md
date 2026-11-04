# 00 — Resumen, alcance, actores y flujos

## 1. Resumen del producto

Plataforma SaaS multitenant para que prestamistas independientes en Perú administren
su negocio de crédito de punta a punta: clientes, solicitudes, evaluación, préstamos,
cronogramas, desembolsos, cobros, caja, cartera vencida, documentos, notificaciones y
reportes.

Cada organización (tenant) opera con datos, usuarios, configuración, numeración y
documentos propios. El aislamiento entre tenants es un requisito de seguridad de primer
nivel, no una característica opcional.

Principios del producto:

1. **El dinero se calcula en el backend.** El frontend solo simula y presenta.
2. **Todo movimiento financiero es trazable e irreversible por borrado.** Se corrige con
   movimientos compensatorios.
3. **Nada se comparte entre tenants**, ni por error de consulta, ni por IDOR, ni por
   procesos en segundo plano.
4. **El sistema no decide reglas legales ni financieras por sí solo.** Las reglas
   configurables se validan contra límites verificados y quedan versionadas por operación.
5. **La operación diaria debe ser rápida en móvil**: registrar un cobro no puede requerir
   más de una pantalla y pocos campos.

## 2. Alcance

### 2.1 Incluido en la versión 1

- Multitenancy con aislamiento lógico (esquema compartido + `tenant_id`), con RLS de
  PostgreSQL como defensa adicional.
- Alta controlada de tenants, membresías, roles y permisos.
- Autenticación con JWT de corta duración + refresh token rotativo revocable.
- Gestión de clientes con búsqueda, detección de duplicados e importación CSV/Excel.
- Solicitudes de crédito con flujo de estados y responsables.
- Préstamos con versiones inmutables de condiciones y cronograma versionado.
- Motor financiero con métodos configurables y pruebas de referencia numéricas.
- Pagos totales, parciales, múltiples cuotas, anticipados y cancelación total.
- Idempotencia, bloqueo por préstamo y aplicación transaccional de pagos.
- Reversos auditados con movimiento compensatorio (sin borrado físico).
- Caja por tenant y por usuario/sucursal, con apertura, cierre y diferencias.
- Cartera, asignación a cobradores, gestiones y promesas de pago.
- Dashboard con definiciones explícitas de cada indicador.
- Reportes filtrables y exportación PDF/Excel, con jobs en segundo plano.
- Documentos PDF privados con enlaces firmados de corta duración.
- WhatsApp Business Platform (API oficial) con outbox, webhooks firmados e historial.
- Configuración por tenant (marca, moneda, zona horaria, feriados, numeración).
- Límites de plan y estados de suscripción (sin cobro recurrente automático).
- Auditoría de operaciones críticas y observabilidad con trazas y métricas.
- Pruebas unitarias, de integración, de aislamiento y E2E.

### 2.2 Excluido explícitamente de la v1

| Excluido | Motivo |
|---|---|
| Cobro recurrente de suscripciones SaaS | Falta definir pasarela, planes y condiciones comerciales |
| Scoring crediticio automático o ML | Requiere datos históricos y validación regulatoria |
| App móvil nativa | La PWA responsive cubre la operación inicial |
| Facturación electrónica SUNAT | Requiere definición tributaria y proveedor OSE |
| Microservicios | No hay necesidad demostrada; se usa monolito modular |
| Multi-país / multi-moneda avanzada | Se modela la moneda, pero v1 opera en PEN por defecto |
| Firma digital de contratos | Requiere proveedor y validación legal |
| Refinanciamiento automático | Solo reprogramación controlada en v1 |

## 3. Actores y roles

### 3.1 Actores de plataforma

| Actor | Responsabilidad | Límite explícito |
|---|---|---|
| Superadministrador de plataforma | Alta/suspensión de tenants, planes, límites, métricas agregadas, configuración global | **No** accede a datos financieros ni personales de un tenant por defecto |
| Soporte de plataforma | Diagnóstico técnico de incidencias | Solo metadatos; acceso a datos requiere concesión temporal “break-glass” |
| Proceso de plataforma (sistema) | Jobs, outbox, recordatorios, exportaciones | Actúa con contexto de tenant explícito y verificado |

### 3.2 Actores de tenant

| Rol | Responsabilidad principal | Restricciones clave |
|---|---|---|
| Propietario (`Owner`) | Configuración del negocio, usuarios, permisos, operaciones y reportes | No puede alterar reglas legales de plataforma |
| Administrador (`Admin`) | Operación general según permisos delegados | No gestiona el plan ni elimina el tenant |
| Analista de créditos (`CreditAnalyst`) | Registra clientes y solicitudes, evalúa y propone | No aprueba ni desembolsa sin permiso explícito |
| Aprobador (`CreditApprover`) | Aprueba o rechaza solicitudes y préstamos | No puede desembolsar si no tiene permiso separado |
| Cajero (`Cashier`) | Cobros, comprobantes, apertura/cierre de su caja | No modifica condiciones del préstamo |
| Cobrador (`Collector`) | Consulta su cartera, registra gestiones y promesas | Registra pagos solo si se le autoriza; ve solo cartera asignada |
| Auditor/Contador (`Auditor`) | Consulta reportes, movimientos y auditoría | Solo lectura; sin operaciones financieras |

Un usuario puede tener más de una membresía (una por tenant) y más de un rol dentro de
un tenant. Los permisos efectivos son la unión de los permisos de sus roles, acotados por
alcance (tenant completo, sucursal o cartera asignada).

### 3.3 Actores externos

- Cliente final: recibe documentos y comunicaciones; **no** accede al sistema en v1.
- Proveedor de WhatsApp Business Platform.
- Proveedor de correo transaccional.
- Almacenamiento privado de objetos.
- Proveedor de observabilidad.

## 4. Flujos principales

### F1. Alta de tenant (controlada)

1. El superadministrador crea el tenant con plan y límites.
2. Se registra la organización y se invita al propietario por correo.
3. El propietario acepta la invitación, crea su contraseña y verifica el correo.
4. Configura identidad visual, moneda, zona horaria, feriados y numeración.
5. El tenant queda `Active`. Si se suspende, se bloquea el acceso operativo pero se
   mantiene lectura/exportación para obligaciones legales.

### F2. Invitación y alta de usuario

1. Un usuario autorizado invita por correo con rol(es) y alcance.
2. La invitación es de un solo uso, con caducidad y hash en base de datos.
3. Al aceptar, se crea/activa la membresía y se registra el evento de auditoría.

### F3. Cliente

1. Búsqueda por documento para detectar duplicados.
2. Alta con datos mínimos; el sistema marca posibles duplicados.
3. Importación CSV/Excel en dos pasos: validación/previsualización y confirmación.
4. Cada fila queda con resultado (`created`, `skipped`, `error`) para trazabilidad.

### F4. Ciclo del crédito

```
Solicitud (Draft) → Pendiente de evaluación → En evaluación
   → Aprobada → (creación de préstamo) → Aprobado → Desembolsado → Vigente
   → Pagado | Cancelado anticipadamente | Anulado (procedimiento)
   Rechazada / Cancelada (desde estados no desembolsados)
```

- La aprobación **no** desembolsa. El desembolso es una operación separada, autorizada,
  que genera préstamo, cronograma, documento y movimiento de caja.
- La mora **no** es un estado del ciclo de vida: es una condición de morosidad calculada
  sobre cuotas vencidas y expuesta como dimensión separada.

### F5. Cobro

1. Apertura de caja (si aplica al tenant).
2. Búsqueda del cliente/préstamo y visualización de cuotas y saldos.
3. Registro del pago con medio, referencia, importe, fecha efectiva e idempotency key.
4. El backend bloquea el préstamo, valida, distribuye el importe, actualiza cuotas,
   genera comprobante y movimiento de caja en **una sola transacción**.
5. Se emite el comprobante PDF y, si corresponde, se encola la notificación.

### F6. Reverso

1. Usuario autorizado solicita reverso con motivo obligatorio.
2. El sistema crea un pago/movimiento compensatorio, revierte aplicaciones, recalcula
   saldos y estados de cuotas.
3. El pago original permanece visible y marcado como `Reversed`.

### F7. Cobranza

1. Asignación de clientes/préstamos a cobradores por ruta o cartera.
2. El cobrador ve cuotas del día y vencidas de **su** cartera.
3. Registra gestión (visita, llamada, mensaje), resultado y promesa de pago.
4. Una promesa **no** es un pago: se muestra en un indicador distinto y se marca
   `Kept`/`Broken` según corresponda.

### F8. Reprogramación

1. Se registra motivo, condiciones anteriores y nuevas.
2. Se crea una nueva versión de condiciones y un nuevo cronograma.
3. El cronograma anterior queda visible e inmutable. No se borran cuotas históricas.

### F9. Documentos y notificaciones

1. Se solicita la generación de un documento.
2. Se genera el PDF, se almacena privado y se registra hash y metadatos.
3. La descarga usa enlace firmado de corta duración.
4. El envío por WhatsApp se encola (outbox) y su estado evoluciona según webhooks.

### F10. Reportes y exportaciones

1. El usuario aplica filtros autorizados.
2. La consulta se ejecuta con aislamiento de tenant.
3. Si el volumen es alto, se genera en segundo plano y se notifica al usuario.
4. Toda exportación incluye tenant, filtros aplicados, fecha de generación y totales.

### F11. Suspensión y salida del tenant

1. Suspensión: bloquea operaciones nuevas, conserva lectura/exportación.
2. Baja: exportación completa de datos y documentos, luego retención/eliminación según
   política legal y contractual.

## 5. Supuestos iniciales (no bloqueantes, revisables)

| ID | Supuesto | Impacto si cambia |
|---|---|---|
| S-01 | Operación inicial en Perú, zona horaria `America/Lima`, locale `es-PE` | Bajo |
| S-02 | Moneda principal PEN; se modela moneda pero no se opera multimoneda en v1 | Medio |
| S-03 | Un tenant puede tener varias sucursales, pero v1 funciona con una por defecto | Bajo |
| S-04 | Interés simple; sin capitalización salvo que se configure y sea legal | **Alto** |
| S-05 | Los préstamos se pagan en cuotas; no hay líneas de crédito revolventes | Medio |
| S-06 | Los clientes no acceden a un portal en v1 | Medio |
| S-07 | El comprobante interno no es comprobante fiscal | Bajo |
| S-08 | WhatsApp usa API oficial con cuenta de negocio por tenant o compartida (decisión pendiente) | **Alto** |
| S-09 | Los feriados se configuran por tenant; no se asume calendario nacional perfecto | Bajo |
| S-10 | Retención de datos: mínimo legal por definir con asesoría; el borrado será lógico y restringido | **Alto** |
| S-11 | El despliegue objetivo aún no se elige (Neon solo aporta PostgreSQL) | Medio |
| S-12 | Un solo idioma de interfaz (es-PE) en v1 | Bajo |

## 6. Glosario

| Término | Definición |
|---|---|
| Tenant | Organización prestamista con datos y configuración aislados |
| Membresía | Vínculo entre un usuario y un tenant, con roles y estado |
| Condiciones (`LoanTerms`) | Versión inmutable de capital, tasa, método, frecuencia, plazo y fechas |
| Cronograma | Conjunto de cuotas generado a partir de una versión de condiciones |
| Cuota (`Installment`) | Obligación programada: capital, interés, otros, total y vencimiento |
| Aplicación (`PaymentAllocation`) | Asignación de parte de un pago a una cuota y concepto |
| Reverso | Operación compensatoria que anula el efecto de un pago sin borrarlo |
| Mora | Interés por pago tardío, calculado según contrato y límites legales |
| Promesa de pago | Compromiso declarado por el cliente; no constituye ingreso |
| Outbox | Tabla transaccional de eventos pendientes de envío externo |
| Idempotencia | Garantía de que una operación repetida no produce doble efecto |
| Devengado vs. cobrado | Interés generado por el calendario vs. interés efectivamente recibido |
