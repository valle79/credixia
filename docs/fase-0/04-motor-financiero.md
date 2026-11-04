# 04 — Motor financiero: reglas, convenciones y casos de referencia

> **Estado: especificación preliminar.** Las decisiones marcadas como *bloqueantes* deben
> confirmarse antes de la Fase 5. Ningún cálculo se considera válido hasta que sus
> resultados coincidan con los casos de referencia y estén cubiertos por pruebas.

## 1. Principios

1. Todo importe se calcula en backend con `decimal`; persistencia `numeric(18,2)`;
   tasas `numeric(12,8)`; intermedios con escala ≥ 6.
2. La tasa **siempre** se acompaña de periodicidad y tipo. “10 %” sin periodicidad es un
   dato inválido.
3. No se convierte ni capitaliza implícitamente. Si el producto es interés simple, no se
   aplica interés compuesto, y viceversa.
4. El cronograma se calcula una vez y se versiona. No se recalcula silenciosamente.
5. La suma de cuotas debe reconciliar con el total contractual; el residuo de redondeo se
   asigna explícitamente a la última cuota.
6. El orden de imputación de pagos es contractual y configurable; no se inventa por código.

## 2. Datos de entrada obligatorios de una operación

| Dato | Ejemplo | Validación |
|---|---|---|
| Capital | 1000.00 | > 0, precisión de moneda |
| Moneda | PEN | ISO 4217, habilitada por tenant |
| Tasa | 2.00000000 | ≥ 0; ≤ límite legal verificado |
| Periodicidad de tasa | Mensual | Anual, mensual, diaria, por cuota |
| Tipo de tasa | Nominal | Nominal / efectiva (según producto) |
| Método | Flat / Francés / Alemán | Habilitado por tenant |
| Frecuencia de pago | Mensual | Independiente de la periodicidad de tasa |
| Plazo | 4 cuotas | > 0, ≤ máximo del plan |
| Fecha de desembolso | 2026-11-02 | Fecha de negocio del tenant |
| Primera cuota | 2026-12-02 | ≥ desembolso + regla |
| Días de gracia | 0 | Solo si el producto lo permite |
| Regla de día no hábil | Mover al siguiente hábil | Configurable |
| Convención de días | Actual/365 | Actual/360, 30/360 |
| Política de redondeo | HALF_UP a 2 decimales | Única por operación |
| Orden de imputación | Configurado | Snapshot en el contrato |

## 3. Métodos de cálculo admitidos

Sea `P` el capital, `i` la tasa por período de pago, `n` el número de cuotas.

### M1 — Interés flat sobre capital original

- Interés total: `I = P × i × n`
- Capital por cuota: `P / n`
- Interés por cuota: `I / n`
- Cuota: `(P + I) / n`
- La tasa no se aplica al saldo; el interés no disminuye con los pagos.

### M2 — Interés sobre saldo con capital constante (alemán)

- Capital por cuota: `P / n`
- Interés de la cuota `k`: `saldo_{k-1} × i`
- Cuota `k`: `capital + interés_k`
- Las cuotas decrecen.

### M3 — Cuota constante sobre saldo (francés)

- Cuota: `A = P × i / (1 − (1 + i)^(−n))`
- Interés `k`: `saldo_{k-1} × i`; capital `k`: `A − interés_k`
- Si `i = 0`, `A = P / n` (solo si el producto permite tasa cero).

**No son equivalentes.** El método se muestra en el contrato, en la simulación y en el
PDF. Un cambio de método exige nueva versión de condiciones.

### 3.1 Conversión de tasas (propuesta, a confirmar)

- Si la tasa está expresada **por período de pago** (ej. “2 % mensual” con cuotas
  mensuales), se usa directamente `i`.
- Si es **nominal anual** y la frecuencia es mensual: `i = tasa_anual / 12` (lineal).
- Si es **nominal anual** y la frecuencia es diaria: `i_diaria = tasa_anual × días / base`
  según convención (`Actual/365`, `Actual/360`, `30/360`).
- Conversiones entre tasas efectivas requieren exponentes y solo se admiten si el producto
  lo declara explícitamente; no se aplican por defecto.
- La periodicidad de la tasa y la frecuencia de pago se muestran por separado.

## 4. Fechas y calendario

- Fecha de negocio = fecha en la zona horaria del tenant (`America/Lima` por defecto).
- Frecuencia diaria: +1 día calendario o hábil según regla.
- Semanal: +7 días. Quincenal: +15 días. Mensual: mismo día del mes con *clamp* al último
  día (31 → 30/28/29).
- Día no hábil: `MoveNext`, `MovePrevious` o `NoAdjust`. La regla se congela en la versión
  de condiciones.
- Feriados por tenant + feriados nacionales configurados. No se asume un calendario
  perfecto: se permite mantenerlo.
- La fecha de la primera cuota no puede ser anterior al desembolso.
- Los días de gracia, si existen, se modelan explícitamente (solo interés o capital +
  interés) y no se confunden con días de atraso.

## 5. Redondeo

Propuesta:

- Cálculo intermedio con escala 8.
- Importes por cuota redondeados a 2 decimales con `HALF_UP` (`MidpointRounding.AwayFromZero`).
- Interés por cuota = `round(saldo × i, 2)` antes de calcular el capital.
- Cuota constante redondeada a 2 decimales; el capital de cada cuota es `cuota − interés`.
- Última cuota: `capital_último = P − Σ capital_previo`; `total_último =
  capital_último + interés_último + otros_último`.
- Invariantes que deben cumplirse siempre:
  - `Σ capital = P`
  - `Σ total = P + Σ interés + Σ otros`
  - `total_k = capital_k + interés_k + otros_k` para toda cuota
- Si una cuota queda con total ≤ 0 por un ajuste, se rechaza la generación y se reporta.

**Bloqueante:** confirmar `HALF_UP` vs. `HALF_EVEN` y la escala de tasa.

## 6. Mora

- Se devenga por día de atraso, interés simple, sin capitalización (salvo que el régimen
  aplicable y el contrato lo permitan de forma expresa).
- Base propuesta: **capital vencido impago** de cuotas vencidas. Alternativas: total de la
  cuota vencida (capital + interés) o capital + interés. **Bloqueante.**
- Fórmula: `mora = base × tasa_mora_diaria × días_de_atraso`, redondeada a 2 decimales al
  momento del cobro.
- Días de atraso: desde el día siguiente al vencimiento hasta la fecha efectiva de pago.
- Tope: tasa de mora ≤ límite legal verificado y ≤ configuración de plataforma. El tenant
  no puede elevar el tope.
- La mora se recalcula al registrar pagos y reversos; nunca se “estima” en el frontend.
- La mora no convierte automáticamente el préstamo en “cancelado”; es un concepto y una
  condición de morosidad.

## 7. Imputación de pagos

El orden es una lista ordenada configurable por producto/tenant, con *snapshot* en el
préstamo. Propuesta inicial: **comisiones → mora → interés → capital**. Alternativas
frecuentes: mora → interés → capital → comisiones; interés → capital → mora.

Reglas:

- Un pago se distribuye en una o más cuotas y conceptos mediante `payment_allocations`.
- No se aplica a conceptos ya cubiertos salvo pago anticipado explícito.
- Pago parcial: deja la cuota `PartiallyPaid` con saldos actualizados.
- Pago de varias cuotas: se aplica en orden de vencimiento.
- Pago anticipado: se aplica a cuotas futuras; si cancela todo, se calcula saldo de
  capital + interés devengado + mora + comisiones, sin penalidad salvo que se configure.
- Sobrepago: se rechaza por defecto. Solo se admite si el producto tiene “saldo a favor”
  habilitado (posterior a v1).
- El importe del pago debe reconciliar: `importe = Σ aplicaciones (+ saldo a favor)`.

## 8. Pago, reverso y reconciliación

- Un pago confirmado es inmutable. Corregir = reverso con motivo y movimiento compensatorio.
- El reverso restaura saldos de cuotas, recalcula mora y estados, y genera movimientos de
  caja inversos. El pago original queda `Reversed` con referencia al reverso.
- No se puede reversar un pago ya reversado.
- No se puede reversar si el dinero ya fue conciliado/cerrado sin autorización adicional
  (regla a definir en Fase 5).
- Invariantes de reconciliación:
  - `saldo_capital = Σ (capital_due − capital_paid)` de cuotas no anuladas.
  - `total_cobrado = Σ pagos confirmados − Σ reversos`.
  - `caja_esperada = saldo_inicial + ingresos − egresos`.
  - Intereses cobrados ≠ intereses programados ≠ intereses devengados.

## 9. Casos de referencia numéricos

Todos usan redondeo HALF_UP a 2 decimales y moneda PEN. Se convertirán en pruebas
automatizadas en la Fase 4.

### C-01 Flat, 4 cuotas mensuales

`P=1000, i=2 % por cuota, n=4`

| Cuota | Capital | Interés | Total |
|---|---|---|---|
| 1 | 250.00 | 20.00 | 270.00 |
| 2 | 250.00 | 20.00 | 270.00 |
| 3 | 250.00 | 20.00 | 270.00 |
| 4 | 250.00 | 20.00 | 270.00 |
| **Σ** | **1000.00** | **80.00** | **1080.00** |

### C-02 Francés, 4 cuotas mensuales

`P=1000, i=2 % mensual, n=4`; cuota calculada 262.6238 → redondeada 262.62.

| Cuota | Saldo inicial | Interés | Capital | Total | Saldo final |
|---|---|---|---|---|---|
| 1 | 1000.00 | 20.00 | 242.62 | 262.62 | 757.38 |
| 2 | 757.38 | 15.15 | 247.47 | 262.62 | 509.91 |
| 3 | 509.91 | 10.20 | 252.42 | 262.62 | 257.49 |
| 4 | 257.49 | 5.15 | 257.49 | 262.64 | 0.00 |
| **Σ** | | **50.50** | **1000.00** | **1050.50** | |

La última cuota absorbe el ajuste de redondeo (262.64).

### C-03 Alemán, 4 cuotas mensuales

`P=1000, i=2 % mensual, n=4`, capital constante 250.

| Cuota | Capital | Interés | Total |
|---|---|---|---|
| 1 | 250.00 | 20.00 | 270.00 |
| 2 | 250.00 | 15.00 | 265.00 |
| 3 | 250.00 | 10.00 | 260.00 |
| 4 | 250.00 | 5.00 | 255.00 |
| **Σ** | **1000.00** | **50.00** | **1050.00** |

### C-04 Diario flat

`P=500, tasa diaria 0.1 %, 30 días, 30 cuotas`
Interés total `= 500 × 0.001 × 30 = 15.00`; cuota diaria `= 515.00 / 30 = 17.1666…`
→ 17.17 (29 cuotas) y última 17.07, con Σ = 515.00. Se valida la reconciliación exacta.

### C-05 Ajuste de última cuota

`P=100, i=1 % por cuota, n=3` (flat)
Interés total 3.00; capital 33.3333 → cuotas 1 y 2: 33.33 + 1.00 = 34.33;
cuota 3: 33.34 + 1.00 = 34.34. Σ = 103.00.

### C-06 Pago parcial

Sobre C-01, pago de 100.00 en la cuota 1 con orden comisiones(0) → mora(0) → interés → capital:
interés 20.00, capital 80.00. Cuota 1 queda: interés pagado 20.00, capital pagado 80.00,
capital pendiente 170.00, estado `PartiallyPaid`. Total pendiente del préstamo: 980.00.

### C-07 Mora

Capital vencido impago 500.00, tasa de mora diaria 0.05 %, 10 días de atraso:
`mora = 500 × 0.0005 × 10 = 2.50`. Si el tope legal/configurado es menor, se aplica el tope.

### C-08 Cancelación anticipada (ilustrativa)

Saldo de capital 600.00 + interés devengado 12.30 + mora 2.50 + comisiones 0.00
= **614.80**. El cálculo definitivo lo realiza el backend con las condiciones vigentes.

### C-09 Tasa cero permitida

`P=1200, i=0, n=4` → cuota 300.00; Σ interés 0.00; Σ total 1200.00. El producto debe tener
`allow_zero_rate = true`; si no, se rechaza con error de validación.

### C-10 Reprogramación

Saldo de capital 700.00, nueva tasa 1.5 % mensual, 5 cuotas, método francés. Se crea la
versión 2 de condiciones y un nuevo cronograma; el cronograma anterior queda `Superseded`
y sus cuotas conservan pagos históricos.

## 10. Casos de prueba obligatorios (matriz)

| # | Caso | Tipo | Resultado esperado |
|---|---|---|---|
| 1 | Préstamo de importe pequeño (P=1.00) | U | Reconciliación exacta; sin cuotas en cero |
| 2 | Muchas cuotas (n=360) | U | Rendimiento aceptable; Σ exacta |
| 3 | Frecuencia diaria, semanal, quincenal, mensual | U | Fechas correctas y sin duplicados |
| 4 | Meses de 28/29/30/31 días | U | *Clamp* correcto; sin fechas inválidas |
| 5 | Vencimiento en fin de semana/feriado | U | Regla configurada aplicada de forma consistente |
| 6 | Tasa cero permitida y no permitida | U | Genera / rechaza según bandera |
| 7 | Flat vs. francés vs. alemán | U | Coinciden con C-01, C-02, C-03 |
| 8 | Última cuota con ajuste | U | C-05 exacto |
| 9 | Pago parcial | U, I | C-06; saldos y estado correctos |
| 10 | Pago anticipado | U, I | Se aplica a futuras; sin duplicar intereses |
| 11 | Cancelación anticipada | U, I | Saldo de capital + devengado + mora + comisiones |
| 12 | Reprogramación | I | Versiones y cronogramas anteriores preservados |
| 13 | Cuotas vencidas | U, I | Estado de morosidad derivado correcto |
| 14 | Intereses de mora | U, I | C-07 y tope aplicado |
| 15 | Condiciones no permitidas | U | Tasa negativa, plazo 0, tasa sobre tope, método no habilitado → rechazo |
| 16 | Generación repetida | I | No duplica cuotas (UNIQUE + idempotencia) |
| 17 | Pago duplicado | I | Un solo pago; misma respuesta idempotente |
| 18 | Dos cobros concurrentes | I | Sin sobrepago ni saldo negativo |
| 19 | Reverso total y parcial | I, E | Saldos restaurados; auditoría completa |
| 20 | Reconciliación global | I | Σ capital, interés y caja cuadran con movimientos |

## 11. Decisiones financieras que deben confirmarse (bloqueantes)

| ID | Decisión | Opciones | Propuesta |
|---|---|---|---|
| DF-01 | Métodos en v1 | Flat / Francés / Alemán / otros | Flat + Francés + Alemán |
| DF-02 | Tipo de interés | Simple / compuesto | Simple; compuesto solo si se documenta y es legal |
| DF-03 | Conversión de tasas | Lineal nominal / efectiva equivalente | Lineal para nominal; no convertir efectivas en v1 |
| DF-04 | Convención de días | Actual/365, Actual/360, 30/360 | Actual/365 con opción por producto |
| DF-05 | Periodicidad de tasa vs. frecuencia | Independientes | Independientes y explícitas |
| DF-06 | Redondeo | HALF_UP / HALF_EVEN; escala | HALF_UP, 2 decimales, residuo en última cuota |
| DF-07 | Regla de día no hábil | MoveNext / MovePrevious / NoAdjust | MoveNext configurable |
| DF-08 | Gracias | Ninguna / solo interés / total | Solo si el producto lo habilita |
| DF-09 | Base de mora | Capital vencido / cuota vencida / capital+interés | Capital vencido impago |
| DF-10 | Orden de imputación | Comisiones/mora/interés/capital y variantes | Comisiones → mora → interés → capital |
| DF-11 | Sobrepago | Rechazar / saldo a favor | Rechazar en v1 |
| DF-12 | Penalidad por prepago | Sí / No | No, salvo configuración |
| DF-13 | Tope de tasas | Fuente y ámbito legal | Configuración de plataforma, no del tenant |
| DF-14 | Moneda | PEN / multimoneda | PEN en v1, modelo preparado |
| DF-15 | Tasa cero | Permitida / prohibida | Configurable por producto |

Cualquier cambio posterior a estas decisiones exige ADR y pruebas de regresión financiera.
