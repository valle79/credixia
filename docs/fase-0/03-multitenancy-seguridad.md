# 03 — Multitenancy, seguridad, privacidad y marco legal

> Este documento identifica riesgos y requisitos. **No constituye asesoría legal.** Las
> conclusiones jurídicas deben validarse con un abogado competente antes de operar con
> clientes reales.

## 1. Estrategia de aislamiento multitenant

### 1.1 Modelo elegido

Esquema compartido, una base de datos, `tenant_id` obligatorio y **tres capas** de control:

1. **Resolución del contexto**: el tenant se deriva de la sesión autenticada y de una
   membresía activa verificada en servidor. Nunca de un header, query string o body.
2. **Aplicación**: filtros globales de EF Core + validación explícita en cada caso de uso +
   FK compuestas que impiden referencias cruzadas.
3. **Base de datos**: Row-Level Security (RLS) de PostgreSQL con `FORCE ROW LEVEL SECURITY`
   y un rol de aplicación que no es dueño de las tablas.

Se descarta base o esquema por tenant en v1 por coste operativo y de migraciones; el
modelo compartido es aceptable **solo** si se demuestra el aislamiento con pruebas.

### 1.2 Flujo del contexto

```mermaid
sequenceDiagram
  participant U as Usuario
  participant API as API
  participant DB as PostgreSQL
  U->>API: POST /auth/login (email, password)
  API->>DB: verificar credenciales
  API-->>U: access token (15 min) + refresh opaco
  U->>API: POST /auth/tenant-context (tenantId)
  API->>DB: validar membresía activa + tenant activo
  API-->>U: access token con tid (tenant activo)
  U->>API: GET /customers (Bearer)
  API->>DB: validar sesión + membresía (caché corta)
  API->>DB: BEGIN; SET LOCAL app.tenant_id = tid; SELECT ...
  DB-->>API: solo filas del tenant
```

- Token de acceso: JWT firmado, 10–15 min, claims mínimos (`sub`, `tid`, `sid`, `ver`).
  Los permisos se resuelven en servidor, no se confían del token.
- Refresh: token opaco de 256 bits, almacenado como hash SHA-256, rotación en cada uso y
  detección de reuso (revoca la familia).
- Si el usuario cambia de tenant, se emite un nuevo access token; el anterior se invalida
  por `sid`/versión de sesión.

### 1.3 Reglas obligatorias de implementación

1. `tenant_id` `NOT NULL` en toda tabla de negocio, con FK compuesta a su padre.
2. Ningún repositorio, query o `FromSql` puede omitir el filtro de tenant.
3. Los `SaveChanges` validan que toda entidad modificada pertenece al tenant del contexto;
   si no, abortan la transacción y auditan.
4. Los jobs reciben `tenant_id` explícito en el payload, lo validan y lo fijan en la sesión
   de BD; un job sin tenant válido falla cerrado.
5. Los documentos se guardan con prefijo `tenant/{tenant_id}/...`; la descarga exige
   autorización y URL firmada de corta duración.
6. Reportes y exportaciones usan las mismas políticas; no existe “modo admin” que las evite.
7. El superadministrador de plataforma no consulta datos de negocio sin concesión
   `break-glass` auditada.

### 1.4 Row-Level Security: diseño y trampas

- Rol de aplicación `lending_app` sin privilegios de dueño; tablas con
  `FORCE ROW LEVEL SECURITY`.
- Política por tabla: `USING (tenant_id = current_setting('app.tenant_id', true)::uuid)`
  y `WITH CHECK` equivalente para escrituras.
- El contexto se establece con `SET LOCAL` **dentro de una transacción**. `SET LOCAL` es
  seguro con PgBouncer/pooling en modo transacción; los `SET` a nivel de conexión **no**.
- Si `app.tenant_id` no está definido, la política devuelve cero filas (fallo cerrado).
- Migraciones y tareas administrativas usan un rol distinto, nunca el rol de aplicación.
- Se prueba explícitamente: conexión reutilizada del pool, transacción sin contexto,
  transacción con contexto de A que intenta leer B, y escritura con `tenant_id` de B.

### 1.5 Pruebas de aislamiento (obligatorias en CI)

Para **cada** endpoint que acepte un ID:

1. Crear tenant A y tenant B con datos equivalentes.
2. Autenticarse como usuario de A.
3. Solicitar el recurso de B por ID, por búsqueda, por filtro y por exportación.
4. Esperar `404` (preferido, evita enumeración) o `403`, nunca datos de B.
5. Verificar que el intento queda auditado cuando corresponde.
6. Repetir con un token de A manipulando `tenant_id`/claims/headers.

La suite también cubre jobs, documentos, webhooks y reportes.

## 2. Modelo de amenazas (resumen)

| ID | Amenaza | Vector | Impacto | Mitigación |
|---|---|---|---|---|
| T-01 | Fuga entre tenants | Query sin filtro, IDOR, exportación | Crítico | Contexto de sesión, filtros, FK compuestas, RLS, suite de aislamiento |
| T-02 | Escalada de privilegios | Rol/claim manipulado, membresía inactiva | Crítico | Autorización en servidor por operación y recurso; revalidación de membresía |
| T-03 | Robo de sesión | XSS, token en localStorage | Alto | Access token en memoria; refresh httpOnly rotativo; CSP; sin tokens en URL |
| T-04 | Reuso de refresh | Token filtrado | Alto | Rotación + detección de reuso + revocación por familia |
| T-05 | Doble pago | Doble clic, reintento, red inestable | Crítico | Idempotency-Key + UNIQUE + transacción + bloqueo |
| T-06 | Sobrepago / saldo negativo | Concurrencia | Crítico | `FOR UPDATE`, validación de saldo, CHECK en BD |
| T-07 | Manipulación de importes/estados | Body manipulado | Crítico | DTOs sin campos protegidos; cálculo en backend |
| T-08 | Borrado de evidencia | DELETE/UPDATE de pagos o auditoría | Crítico | Sin endpoints de borrado; permisos mínimos; auditoría append-only |
| T-09 | Acceso excesivo del superadmin | Curiosidad/abuso interno | Alto | Break-glass con motivo, plazo, aprobación y auditoría |
| T-10 | Fuga de PII en logs/PDF | Logging descuidado, enlaces públicos | Alto | Scrubbing, redacción, URLs firmadas, bucket privado |
| T-11 | Webhook falso | Replay/forgery de WhatsApp | Medio | Verificación de firma, timestamp, deduplicación |
| T-12 | Malware en adjuntos | Carga de archivos | Alto | Validación de tipo real, antivirus, tamaño, almacenamiento aislado |
| T-13 | SSRF | URLs de logo/webhooks configurables | Medio | Lista de hosts permitidos, sin fetch arbitrario |
| T-14 | Inyección SQL | Filtros/orden dinámicos | Alto | Parámetros, listas blancas, EF Core, revisión de SQL crudo |
| T-15 | Fuerza bruta / credential stuffing | Login | Alto | Rate limit, bloqueo progresivo, MFA para roles sensibles (fase posterior) |
| T-16 | Fuga por respaldos | Dumps sin cifrar | Alto | Cifrado en reposo, acceso restringido, pruebas de restauración |
| T-17 | Job con contexto incorrecto | Payload manipulado | Crítico | Tenant explícito validado + RLS + pruebas |
| T-18 | Dependencia comprometida | Supply chain | Alto | Lockfiles, escaneo, actualizaciones, SBOM |
| T-19 | Abuso de cobranza | Comunicaciones sin consentimiento | Alto (legal) | Consentimiento, plantillas aprobadas, baja, límites de frecuencia |
| T-20 | Tasas ilegales | Configuración del tenant | Crítico (legal) | Límites de plataforma no editables por tenant + auditoría |

## 3. Controles OWASP Top 10 (versión vigente al implementar)

| Categoría | Controles previstos |
|---|---|
| A01 Broken Access Control | Autorización por tenant/recurso/operación, denegación por defecto, pruebas negativas, IDs no enumerables (UUID), 404 en accesos cruzados |
| A02 Fallas criptográficas | TLS 1.2+, HSTS, hash de contraseñas con Argon2id o PBKDF2 fuerte, hashes de refresh/invitaciones, cifrado en reposo, secretos en gestor |
| A03 Inyección | Consultas parametrizadas, EF Core, listas blancas de orden/filtros, validación de entrada, sin SQL concatenado |
| A04 Diseño inseguro | Modelado de amenazas, límites de negocio, idempotencia, estados explícitos, revisión de flujos financieros |
| A05 Configuración incorrecta | CORS explícito, headers de seguridad, CSP, sin stack traces, entornos separados, RLS forzado |
| A06 Componentes vulnerables | Escaneo de dependencias en CI, lockfiles, política de actualización, SBOM |
| A07 Fallas de autenticación | Rate limit, bloqueo, refresh rotativo, revocación, verificación de correo, MFA para roles críticos (fase posterior) |
| A08 Integridad de software/datos | Firma de artefactos, migraciones versionadas, auditoría, outbox, validación de webhooks |
| A09 Logging y monitoreo | Logs estructurados, correlación, alertas, auditoría de operaciones críticas, detección de anomalías |
| A10 SSRF | Sin fetch de URLs arbitrarias; allowlist para integraciones salientes |

Controles adicionales: límite de tamaño de request, timeouts, compresión controlada,
protección CSRF según mecanismo de sesión, CSP, `X-Content-Type-Options`, `Referrer-Policy`,
`Permissions-Policy`, y separación de roles de BD.

## 4. Privacidad y protección de datos (Perú)

### 4.1 Normas a validar (no interpretadas aquí)

| Norma | Relevancia | Estado |
|---|---|---|
| Ley N.º 29733 — Protección de Datos Personales | Base del tratamiento, consentimiento, derechos, seguridad | Vigente — validar |
| Reglamento aprobado por D.S. N.º 016-2024-JUS | Nuevas obligaciones, plazos y procedimientos | **Verificar vigencia y cronograma de entrada en vigor** |
| Ley N.º 29571 — Código de Protección y Defensa del Consumidor | Transparencia, información, cobranza, trato al consumidor | Vigente — validar alcance |
| Ley N.º 31143 — protección frente a la usura | Límites y prácticas en crédito de consumo | **Validar alcance por tipo de prestamista y producto** |
| Ley N.º 28587 y normas SBS de transparencia | Aplican a entidades supervisadas; verificar si el tenant lo es | Depende del tenant |
| Ley N.º 26702 — Ley General del Sistema Financiero | Aplicable solo a entidades autorizadas | Depende del tenant |
| Circulares del BCRP sobre tasas máximas | Fijan topes para el sistema financiero | **Verificar vigencia y ámbito** |
| Código Civil, arts. sobre intereses (1242 y ss.) | Interés máximo convencional en operaciones no reguladas | **Validar con abogado** |
| Normas de cobranza y comunicación | Horarios, terceros, frecuencia, lenguaje | **Validar según régimen aplicable** |
| Normas tributarias (SUNAT) | Comprobantes e IGV si corresponde | Fuera de alcance v1; validar |

Ninguna tasa, tope, permiso o exención se codifica como constante “legal” sin fuente
oficial verificada y fecha de vigencia.

### 4.2 Requisitos de diseño derivados

1. **Base legal y consentimiento** registrados por finalidad (operación, cobranza,
   comunicaciones, marketing). Marketing queda **apagado** por defecto.
2. **Minimización**: solo los campos necesarios; documentos de identidad y teléfonos con
   acceso restringido y enmascarados en pantalla según permiso.
3. **Derechos del titular**: procedimiento para acceso, rectificación, cancelación,
   oposición y los derechos adicionales del reglamento vigente, con plazos y trazabilidad.
4. **Registro de bancos de datos** ante la autoridad competente: verificar si aplica a la
   plataforma y/o a cada tenant, y quién es el responsable de cada banco.
5. **Transferencia internacional**: Neon, WhatsApp/Meta, correo y observabilidad pueden
   implicar transferencia; requiere base legal y contratos con encargados.
6. **Retención**: definir plazos por tipo (financiero, comunicaciones, auditoría, logs) y
   procedimiento de eliminación segura. **No se elimina** información sujeta a obligación
   legal o defensa de reclamaciones.
7. **Seguridad**: cifrado en tránsito y reposo, control de acceso, registro de incidentes.
8. **Notificación de incidentes**: procedimiento y plazos por confirmar según normativa.
9. **Encargados de tratamiento**: DPA/contratos con proveedores, confidencialidad,
   subencargados y ubicación de datos.
10. **Privacidad desde el diseño**: campos sensibles fuera de logs, URLs y métricas.

### 4.3 Preguntas que requieren respuesta jurídica antes de producción

- ¿Qué figura jurídica tiene cada tenant (persona natural, empresa no supervisada, entidad
  supervisada) y qué régimen le aplica?
- ¿Qué tasas máximas (compensatoria y moratoria) rigen para cada producto y tenant?
- ¿Se permite capitalizar intereses en el producto concreto?
- ¿Qué requisitos de transparencia y documentación contractual aplican?
- ¿Qué límites de cobranza (horarios, terceros, frecuencia) deben implementarse?
- ¿La plataforma y/o cada tenant deben registrar bancos de datos? ¿Ante quién?
- ¿Qué plazos de retención y eliminación aplican?
- ¿Qué obligaciones tributarias y de comprobantes corresponden?
- ¿Se requiere autorización específica para operar como prestamista en el modelo previsto?

## 5. Acceso excepcional de plataforma (break-glass)

- Requiere solicitud con motivo, tenant, alcance y duración (máx. 24 h).
- Requiere aprobación de un segundo actor autorizado.
- Concede permiso de **solo lectura** y solo a los recursos declarados.
- Genera eventos de auditoría de inicio, cada acceso y cierre.
- Se notifica al propietario del tenant.
- Sin concesión activa, el superadministrador recibe `403` en datos de negocio.

## 6. Respuesta a incidentes (borrador)

1. Detección y clasificación (fuga de datos, indisponibilidad, integridad financiera).
2. Contención: revocar sesiones, rotar credenciales, aislar tenant si aplica.
3. Erradicación y recuperación con respaldo verificado.
4. Evaluación de notificación a titulares y autoridad según normativa.
5. Post-mortem sin culpabilización y acciones correctivas con responsable y fecha.
6. Registro del incidente en el sistema de auditoría/operación.
