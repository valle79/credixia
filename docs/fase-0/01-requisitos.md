# 01 — Requisitos y matriz de aceptación

Prioridad: **M** = Must (v1), **S** = Should (v1 si el cronograma lo permite), **C** = Could (posterior).
Fase: fase de implementación estimada (ver `05-plan-riesgos-decisiones.md`).
“Prueba” indica el tipo mínimo: `U` unitaria, `I` integración, `A` aislamiento/seguridad, `E` E2E.

## 1. Requisitos funcionales

### 1.1 Identidad, sesión y organización

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-AUTH-01 | Alta controlada de organización (tenant) | M | 2 | Solo un actor de plataforma autorizado crea tenants; se registra actor, fecha y plan | I, A |
| RF-AUTH-02 | Inicio de sesión con correo y contraseña | M | 1 | Credenciales válidas emiten access token (≤15 min) y refresh token; credenciales inválidas devuelven 401 sin revelar si el correo existe | I, A |
| RF-AUTH-03 | Refresh con rotación y detección de reuso | M | 1 | Un refresh usado dos veces revoca toda la familia y exige nuevo inicio de sesión | I, A |
| RF-AUTH-04 | Cierre de sesión y revocación por dispositivo | M | 1 | El refresh revocado no puede canjearse; el access token expira en ≤15 min | I |
| RF-AUTH-05 | Recuperación y cambio de contraseña | M | 2 | Token de un solo uso, hash en BD, caducidad ≤30 min; cambio invalida sesiones previas según política | I |
| RF-AUTH-06 | Verificación de correo e invitaciones | M | 2 | Invitación de un solo uso con caducidad; aceptar crea membresía activa | I, E |
| RF-AUTH-07 | Protección contra fuerza bruta | M | 2 | Tras N intentos fallidos por cuenta/IP se bloquea temporalmente y se audita | I, A |
| RF-AUTH-08 | Suspensión/desactivación de usuario | M | 2 | Usuario suspendido no puede autenticarse ni usar refresh vigente | I, A |
| RF-AUTH-09 | Roles y permisos por tenant | M | 2 | Permisos efectivos calculados en servidor; cambios se aplican sin reiniciar el token | I, A |
| RF-AUTH-10 | Selección de tenant activo validada | M | 2 | El tenant se toma de la sesión; un `tenant_id` enviado por el cliente se ignora o se rechaza | A |

### 1.2 Multitenancy

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-TEN-01 | Toda entidad de negocio lleva `tenant_id` no nulo | M | 1 | Restricción NOT NULL y FK compuesta impiden registros huérfanos | I |
| RF-TEN-02 | Consultas filtradas por tenant | M | 1 | Un usuario del tenant A no obtiene por ID, búsqueda, reporte ni exportación datos del tenant B | A |
| RF-TEN-03 | Escrituras cruzadas imposibles | M | 1 | Insertar un préstamo con cliente de otro tenant falla en aplicación **y** en base de datos | I, A |
| RF-TEN-04 | Procesos en segundo plano con contexto explícito | M | 6 | Un job sin tenant válido falla cerrado; no procesa datos de otros tenants | I, A |
| RF-TEN-05 | Archivos aislados por tenant | M | 7 | Un enlace o ID de documento de otro tenant devuelve 404/403 y se audita el intento | A |
| RF-TEN-06 | RLS como defensa adicional | S | 2 | Con el rol de aplicación, una consulta sin contexto de tenant devuelve 0 filas o error | I, A |
| RF-TEN-07 | Suspensión de tenant | M | 9 | Tenant suspendido no permite operaciones nuevas; lectura/exportación según política | I |

### 1.3 Clientes

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-CUS-01 | CRUD de clientes con código interno único por tenant | M | 3 | No se permiten dos clientes con el mismo código en el mismo tenant; sí en tenants distintos | I |
| RF-CUS-02 | Búsqueda por documento, nombre, teléfono y código | M | 3 | Resultados paginados y filtrados por tenant; sin coincidencias cruzadas | I, A |
| RF-CUS-03 | Detección de posibles duplicados | M | 3 | Al crear/importar, coincidencia por documento o nombre+teléfono genera advertencia bloqueante o confirmable según regla | U, I |
| RF-CUS-04 | Historial de préstamos, pagos y comunicaciones | M | 3 | El detalle consolida solo operaciones del cliente y tenant correctos | I |
| RF-CUS-05 | Importación CSV/Excel con previsualización | M | 3 | Se muestra fila, error y duplicado antes de confirmar; la confirmación reporta resultado por fila | U, I, E |
| RF-CUS-06 | Documentos del cliente privados | M | 7 | Carga con validación de tipo real y tamaño; descarga con enlace firmado y autorización | I, A |
| RF-CUS-07 | Minimización de datos personales | M | 3 | Solo se almacenan campos definidos; campos opcionales no obligatorios | Revisión |

### 1.4 Solicitudes de crédito

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-APP-01 | Registrar solicitud en borrador | M | 3 | Se guarda con cliente, monto, moneda, frecuencia, plazo, tasa propuesta y método | I, E |
| RF-APP-02 | Transiciones de estado auditadas | M | 3 | Solo transiciones permitidas; cada cambio registra actor, fecha y motivo | U, I |
| RF-APP-03 | Permisos separados para evaluar, aprobar, rechazar y desembolsar | M | 3 | Un analista sin permiso de aprobación recibe 403 aunque manipule la UI | A |
| RF-APP-04 | Aprobación no desembolsa | M | 3 | Tras aprobar, no existe préstamo desembolsado ni movimiento de caja | I, E |
| RF-APP-05 | Adjuntos y evaluación documentados | M | 3 | La solicitud conserva observaciones, responsable y documentos | I |

### 1.5 Préstamos y condiciones

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-LOAN-01 | Código de préstamo único por tenant | M | 5 | Restricción UNIQUE `(tenant_id, code)` | I |
| RF-LOAN-02 | Condiciones versionadas e inmutables tras desembolso | M | 5 | Editar condiciones desembolsadas no altera la versión original; se crea nueva versión con motivo | U, I |
| RF-LOAN-03 | Aprobación y desembolso separados | M | 5 | El desembolso exige permiso propio y genera cronograma, documento y movimiento de caja | I, E |
| RF-LOAN-04 | Ciclo de vida y morosidad como ejes separados | M | 5 | Un préstamo vigente puede estar en mora sin cambiar su estado de ciclo de vida | U |
| RF-LOAN-05 | Anulación solo por procedimiento controlado | M | 5 | No existe borrado físico; la anulación exige motivo, autorización y auditoría | I |
| RF-LOAN-06 | Reprogramación conserva historial | M | 6 | Quedan visibles condiciones y cronograma anteriores y los nuevos | I |

### 1.6 Motor financiero y cronogramas

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-FIN-01 | Cálculo determinista con `decimal` | M | 4 | Los mismos insumos producen el mismo resultado; no se usa punto flotante | U |
| RF-FIN-02 | Métodos de interés configurables y explícitos | M | 4 | Cada préstamo registra método, tasa, periodicidad y convención usados | U, I |
| RF-FIN-03 | Frecuencias diaria, semanal, quincenal, mensual y personalizada | M | 4 | Fechas de vencimiento correctas según calendario y feriados configurados | U |
| RF-FIN-04 | Redondeo y ajuste de última cuota | M | 4 | Σ cuotas = total contractual; la diferencia residual queda explícita en la última cuota | U |
| RF-FIN-05 | Reconciliación de capital e interés | M | 4 | Σ capital programado = capital desembolsado; Σ intereses = interés contractual | U |
| RF-FIN-06 | Generación idempotente | M | 4 | Reejecutar la generación no duplica cuotas; UNIQUE por préstamo/versión/número | I |
| RF-FIN-07 | Tasa cero solo si se permite | M | 4 | Producto con tasa cero habilitada genera cronograma válido; si no, se rechaza | U |
| RF-FIN-08 | Límites legales configurables | M | 4 | Una tasa sobre el límite verificado se rechaza y se audita; el usuario del tenant no puede elevar el límite | U, I, A |
| RF-FIN-09 | Simulación sin persistencia | S | 4 | El endpoint de simulación devuelve el mismo resultado que el cálculo definitivo sin crear registros | U, I |
| RF-FIN-10 | Mora configurable y acotada | M | 6 | La mora se calcula solo sobre la base definida y nunca excede el límite configurado | U, I |

### 1.7 Pagos, aplicaciones y reversos

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-PAY-01 | Registrar pago total, parcial, múltiple y anticipado | M | 5 | Cada caso actualiza cuotas y saldos correctamente según el orden de imputación configurado | U, I, E |
| RF-PAY-02 | Idempotencia en cobros | M | 5 | Repetir la misma `Idempotency-Key` devuelve la misma respuesta y no crea otro pago | I |
| RF-PAY-03 | Transacción única de pago | M | 5 | Pago, aplicaciones, cuotas, saldo, caja y comprobante se confirman o revierten juntos | I |
| RF-PAY-04 | Concurrencia segura | M | 5 | Dos pagos simultáneos no sobrepagan ni dejan saldos inconsistentes | I |
| RF-PAY-05 | Sin borrado de pagos confirmados | M | 5 | No existe endpoint de DELETE; la corrección es por reverso | I, A |
| RF-PAY-06 | Reverso auditado | M | 5 | Motivo, actor, fecha, referencia y movimiento compensatorio quedan registrados | I, E |
| RF-PAY-07 | Numeración de operación única por tenant | M | 5 | Restricción UNIQUE `(tenant_id, operation_number)` | I |
| RF-PAY-08 | Comprobante interno | M | 5 | Se genera PDF con número, fecha, importe, distribución y datos del tenant | I |

### 1.8 Caja y desembolsos

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-CASH-01 | Apertura y cierre de caja | M | 6 | Solo una sesión abierta por caja/usuario; cierre calcula saldo esperado y diferencia | I, E |
| RF-CASH-02 | Movimientos inmutables | M | 6 | Ingresos, egresos, cobros, desembolsos y reversos generan movimientos trazables | I |
| RF-CASH-03 | Desembolso con validaciones | M | 5 | Sin aprobación, caja abierta y permiso, el desembolso se rechaza | I, A |
| RF-CASH-04 | Conciliación explicable | M | 6 | El saldo de caja se explica por la suma de movimientos y su origen | I |

### 1.9 Cobranza

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-COL-01 | Carteras/rutas y asignación | M | 6 | El cobrador solo ve los clientes/préstamos asignados | I, A |
| RF-COL-02 | Cuotas del día y vencidas | M | 6 | Los listados respetan zona horaria, feriados y asignación | I |
| RF-COL-03 | Gestiones de cobranza | M | 6 | Se registra tipo, resultado, fecha, nota y actor | I, E |
| RF-COL-04 | Promesas de pago separadas de pagos | M | 6 | Una promesa no altera saldos ni aparece como cobro | U, I |
| RF-COL-05 | Reasignación autorizada | S | 6 | La reasignación exige permiso y deja historial | I, A |

### 1.10 Dashboard y reportes

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-DASH-01 | Indicadores con definición documentada | M | 8 | Cada tarjeta muestra definición y período; los totales cuadran con reportes detallados | I, E |
| RF-DASH-02 | Separar programado, devengado, cobrado y revertido | M | 8 | Un préstamo no genera “ganancia” por intereses no cobrados | I |
| RF-REP-01 | Reportes mínimos filtrables | M | 8 | Los 18 reportes del alcance existen con filtros y totales | I, E |
| RF-REP-02 | Exportación PDF/Excel | M | 8 | Incluye tenant, filtros, fecha y totales; respeta permisos y tenant | I, A |
| RF-REP-03 | Exportaciones en segundo plano | S | 8 | Un volumen alto se procesa como job y se notifica al usuario | I |

### 1.11 Documentos y notificaciones

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-DOC-01 | PDF con identidad del tenant | M | 7 | Cronograma, resumen, constancia, historial y estado de cuenta incluyen los campos definidos | I |
| RF-DOC-02 | Almacenamiento privado | M | 7 | El bucket no es público; el acceso usa URL firmada de corta duración | I, A |
| RF-NOT-01 | WhatsApp oficial con outbox | M | 7 | El mensaje se registra en outbox en la misma transacción del evento de negocio | I |
| RF-NOT-02 | Estados reales del mensaje | M | 7 | Se distingue solicitado, aceptado, enviado, entregado, leído y fallido | I |
| RF-NOT-03 | Webhook autenticado | M | 7 | Firma inválida se rechaza y se audita; mensajes duplicados no se reprocesan | I, A |
| RF-NOT-04 | Consentimiento y baja | M | 7 | Sin consentimiento vigente no se envía cobranza por el canal; la baja se respeta | I, A |
| RF-NOT-05 | Reintentos y errores | M | 7 | Fallos transitorios reintentan con backoff; fallos definitivos quedan en dead-letter visibles | I |

### 1.12 Configuración, planes y auditoría

| ID | Requisito | Pri | Fase | Criterio de aceptación verificable | Prueba |
|---|---|---|---|---|---|
| RF-CFG-01 | Configuración por tenant | M | 2 | Cada tenant tiene su propia configuración; cambiarla no altera operaciones históricas | I |
| RF-CFG-02 | Feriados y numeración | M | 2 | Feriados afectan el cálculo de vencimientos según la regla configurada | U, I |
| RF-CFG-03 | Logo y datos de documentos | M | 7 | El PDF usa la identidad del tenant correcto | I, A |
| RF-SAAS-01 | Planes y límites configurables | S | 9 | Al exceder un límite, la operación se bloquea con mensaje claro; no se destruyen datos | I |
| RF-SAAS-02 | Estados de suscripción | S | 9 | Suspensión no impide exportar ni consultar datos históricos | I |
| RF-AUD-01 | Auditoría de operaciones críticas | M | 2 | Login, cambios de permisos, desembolsos, pagos, reversos, anulaciones y accesos excepcionales quedan registrados | I, A |
| RF-AUD-02 | Consulta de auditoría filtrable | S | 8 | Solo roles autorizados consultan; los registros son de solo lectura | I, A |
| RF-PLAT-01 | Métricas agregadas de plataforma | S | 9 | El superadministrador ve métricas sin PII ni detalle financiero por tenant | I, A |
| RF-PLAT-02 | Acceso excepcional “break-glass” | S | 9 | Requiere justificación, aprobación, plazo y genera auditoría; sin él, el acceso se deniega | I, A |

## 2. Requisitos no funcionales

| ID | Categoría | Requisito | Criterio de aceptación | Prueba |
|---|---|---|---|---|
| RNF-SEC-01 | Seguridad | Autorización en servidor por tenant, recurso y operación | Pruebas negativas devuelven 403/404 sin filtrar existencia | A |
| RNF-SEC-02 | Seguridad | Secretos fuera del repositorio | `.env.example` sin valores; el pipeline falla si detecta secretos | Revisión, CI |
| RNF-SEC-03 | Seguridad | Protección OWASP Top 10 vigente | Controles mapeados y probados; sin hallazgos críticos abiertos | A |
| RNF-SEC-04 | Seguridad | Rate limiting y límites de tamaño | Endpoints sensibles limitados; cargas rechazadas por tamaño/tipo | I, A |
| RNF-SEC-05 | Seguridad | Logs sin secretos ni PII innecesaria | Revisión de muestras de log y pruebas de scrubbing | Revisión |
| RNF-MT-01 | Aislamiento | Pruebas automáticas de acceso cruzado en todos los módulos | Suite de aislamiento en CI, sin fallos | A |
| RNF-MT-02 | Aislamiento | Contexto de tenant limpiado entre transacciones y conexiones | Prueba con conexión reutilizada del pool | I |
| RNF-COMP-01 | Exactitud | Importes con `decimal`/`numeric`; sin flotantes | Revisión estática + pruebas de referencia | U |
| RNF-COMP-02 | Exactitud | Reconciliación de saldos y totales | Job/prueba de reconciliación sin diferencias | I |
| RNF-PERF-01 | Rendimiento | Cobro registrado < 1 s p95 en condiciones normales | Prueba de carga con dataset representativo | Rendimiento |
| RNF-PERF-02 | Rendimiento | Listados paginados en servidor < 500 ms p95 con 100k filas por tenant | Prueba de carga e índices verificados | Rendimiento |
| RNF-PERF-03 | Escalabilidad | Consultas críticas con índices compuestos por tenant | Planes de ejecución revisados | Revisión |
| RNF-OBS-01 | Observabilidad | Logs estructurados con correlation ID | Traza completa de una operación de pago | I |
| RNF-OBS-02 | Observabilidad | Health checks, métricas y alertas | `/health/live` y `/health/ready`; alertas de error 5xx y outbox atascado | I |
| RNF-OBS-03 | Observabilidad | Trazas distribuidas API→DB→jobs | OpenTelemetry exporta trazas al proveedor elegido | I |
| RNF-BKP-01 | Continuidad | RPO ≤ 15 min y RTO ≤ 4 h (propuesta, pendiente de confirmar) | Simulacro de restauración documentado y exitoso | Operación |
| RNF-BKP-02 | Continuidad | Migraciones con rollback/forward-fix probado | Ensayo en staging con respaldo previo | Operación |
| RNF-UX-01 | UX | Registro de cobro en móvil en ≤ 3 pasos | Prueba de usabilidad con tarea cronometrada | E, UX |
| RNF-UX-02 | UX | Estados de carga, vacío y error en todas las vistas | Revisión de componentes y pruebas E2E | E |
| RNF-ACC-01 | Accesibilidad | Navegación por teclado y contraste AA | Auditoría automatizada + revisión manual | E |
| RNF-MAINT-01 | Mantenibilidad | Clean Architecture con dependencias verificadas | Architecture tests en CI | U (arch) |
| RNF-MAINT-02 | Mantenibilidad | Cobertura de dominio financiero ≥ 90 % | Reporte de cobertura en CI | CI |
| RNF-COMP-03 | Cumplimiento | Reglas legales configurables y no editables por el tenant | Prueba de intento de elevación de límites | A |
| RNF-PORT-01 | Portabilidad | Ejecución local reproducible con Docker Compose | Un comando levanta API, frontend y PostgreSQL local | Operación |

## 3. Trazabilidad requisito → fase → prueba

| Módulo | IDs | Fase | Suite principal |
|---|---|---|---|
| Identidad | RF-AUTH-01..10 | 1–2 | Integration + Isolation |
| Multitenancy | RF-TEN-01..07 | 1–2 | Isolation + Architecture |
| Clientes | RF-CUS-01..07 | 3 | Unit + Integration + E2E |
| Solicitudes | RF-APP-01..05 | 3 | Unit + Integration |
| Préstamos | RF-LOAN-01..06 | 5–6 | Unit + Integration |
| Motor financiero | RF-FIN-01..10 | 4 | Unit (referencia numérica) |
| Pagos | RF-PAY-01..08 | 5 | Unit + Integration + Concurrency + E2E |
| Caja | RF-CASH-01..04 | 5–6 | Integration + E2E |
| Cobranza | RF-COL-01..05 | 6 | Integration + E2E |
| Dashboard/Reportes | RF-DASH-01..02, RF-REP-01..03 | 8 | Integration + E2E |
| Documentos/Notificaciones | RF-DOC-01..02, RF-NOT-01..05 | 7 | Integration + Webhook |
| Configuración/Planes/Auditoría | RF-CFG, RF-SAAS, RF-AUD, RF-PLAT | 2, 7–9 | Integration + Isolation |


## 4. Apéndice A — Definición de indicadores del dashboard

Regla general: un indicador siempre declara **período**, **filtros** y **si es bruto o neto
de reversos**. Ningún indicador se etiqueta como “ganancia” o “utilidad” si incluye
intereses solo programados.

| Indicador | Definición exacta |
|---|---|
| Capital desembolsado | Σ `principal` de préstamos con desembolso confirmado en el período, excluyendo anulados |
| Préstamos vigentes | N.º de préstamos `Disbursed/Active` con saldo de capital > 0 al cierre del período |
| Cartera pendiente | Σ total pendiente (capital + interés + otros) de cuotas no vencidas y no anuladas de préstamos vigentes |
| Cartera vencida | Σ total pendiente de cuotas con `due_date < fecha de negocio` y saldo > 0 |
| Cuotas vencidas | N.º de cuotas vencidas con saldo > 0, no anuladas |
| Monto vencido | Σ total pendiente de las cuotas vencidas |
| Cobros del día / semana / mes | Σ pagos confirmados con `payment_date` en el rango, **neto** de reversos del rango; se muestra también el bruto |
| Próximos vencimientos | Cuotas con `due_date` en los próximos 7/30 días y saldo > 0 |
| Clientes con cuotas pendientes | N.º de clientes distintos con al menos una cuota con saldo > 0 |
| Préstamos pagados | Préstamos con saldo de capital 0 y todas las cuotas `Paid` |
| Préstamos en mora | Préstamos con `delinquency_status = Overdue` y saldo > 0 |
| Promesas pendientes | Promesas `Pending` con fecha comprometida ≥ hoy; no suma dinero |
| Distribución por cobrador | Σ saldo pendiente de préstamos asignados a cada cobrador |
| Evolución histórica de cobros | Σ pagos confirmados netos agrupados por día/semana/mes |
| Interés programado | Σ `interest_due` del cronograma activo de préstamos vigentes |
| Interés devengado | Interés de cuotas con `due_date ≤ fecha de corte`, independientemente de si se cobró |
| Interés cobrado | Σ aplicaciones a concepto `Interest` de pagos confirmados, neto de reversos |
| Interés revertido | Σ aplicaciones a interés anuladas por reversos confirmados |
| Capital recuperado | Σ aplicaciones a concepto `Principal` netas de reversos |
| Saldo a favor | Solo si el producto lo habilita; en v1 no se ofrece |

## 5. Apéndice B — Catálogo mínimo de reportes

| ID | Reporte | Filtros mínimos | Formato |
|---|---|---|---|
| REP-01 | Cartera total | período, sucursal, cobrador, estado | Pantalla, Excel, PDF |
| REP-02 | Cartera vigente | período, sucursal, cobrador | Pantalla, Excel |
| REP-03 | Cartera vencida | rango de atraso, cobrador, zona | Pantalla, Excel, PDF |
| REP-04 | Cronograma de préstamo | préstamo, versión | PDF |
| REP-05 | Historial de pagos por cliente | cliente, rango de fechas | Pantalla, PDF |
| REP-06 | Cobros por día | fecha, caja, cobrador | Pantalla, Excel |
| REP-07 | Cobros por período | rango, sucursal, cobrador | Pantalla, Excel |
| REP-08 | Cobros por cobrador | rango, cobrador | Pantalla, Excel |
| REP-09 | Préstamos desembolsados | rango, sucursal | Pantalla, Excel |
| REP-10 | Préstamos cancelados | rango, tipo de cancelación | Pantalla, Excel |
| REP-11 | Saldos pendientes | rango, cobrador, estado | Pantalla, Excel |
| REP-12 | Intereses programados y cobrados | rango, préstamo | Pantalla, Excel |
| REP-13 | Movimientos de caja | sesión, rango, usuario | Pantalla, PDF |
| REP-14 | Aperturas y cierres de caja | rango, caja, usuario | Pantalla, PDF |
| REP-15 | Anulaciones y reversos | rango, usuario, motivo | Pantalla, Excel |
| REP-16 | Clientes con próximos vencimientos | días, cobrador | Pantalla, Excel |
| REP-17 | Clientes en mora | rango de atraso, cobrador | Pantalla, Excel |
| REP-18 | Actividad de usuarios y auditoría | usuario, acción, rango | Pantalla, Excel |

Todo reporte incluye: tenant, filtros aplicados, fecha/hora de generación, usuario que lo
solicitó y totales verificables. Las exportaciones grandes se procesan como job.