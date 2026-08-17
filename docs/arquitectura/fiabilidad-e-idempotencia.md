# Fiabilidad e idempotencia

- **Fecha:** 2026-07-16 · **Estado:** congelado
- Decisiones: [ADR-T08](../adr/ADR-T08-outbox-transaccional.md) · [ADR-T09](../adr/ADR-T09-inbox-deduplicacion.md) · [ADR-T19](../adr/ADR-T19-resiliencia.md)

---

## 1. Modelo de garantías (terminología precisa)

| Concepto | Qué significa aquí |
|---|---|
| **Entrega at-least-once** | el broker puede entregar el mismo mensaje más de una vez |
| **Inbox por `MessageId`** | deduplicación **de transporte**: descarta la reentrega del mismo mensaje |
| **Idempotencia de negocio** | por **claves naturales** (`(StudentId, CourseId)`, referencia de Finalización) |
| **Ledger por `PurchaseId`** | deduplicación **de negocio** cuando el reintento llega con un `MessageId` **nuevo** |
| **Efecto de negocio effectively-once** | el resultado observable ocurre una sola vez, pese a entregas repetidas |

> **No se afirma “exactly-once delivery”**: no existe. Lo que se garantiza es **consumo at-least-once
> con efecto de negocio effectively-once**, mediante transacción local, deduplicación e idempotencia.

## 2. Transactional Outbox

Evita el *dual write* (confirmar el cambio local y perder la publicación del mensaje). El mensaje se
escribe en el **Outbox dentro de la misma transacción local** que el cambio de estado y se publica
después.

| Servicio | Outbox | Qué publica |
|---|---|---|
| `paid-enrollment` | ✅ | comandos de Saga |
| `payment-provider-sim` | ✅ | replies de las operaciones de pago |
| `enrollment` | ✅ | reply de Saga **y** `EstudianteMatriculado` |
| `learning` | ✅ | `CursoFinalizado` |
| `certification` | ❌ | no publica mensajes externos |
| `course-authoring` | ❌ | sus eventos de dominio no se publican |

## 3. Inbox

| Servicio | Inbox | Qué consume |
|---|---|---|
| `paid-enrollment` | ✅ | replies de pago y de concesión |
| `payment-provider-sim` | ✅ | comandos de pago (**modifica estado: capturar dos veces sería cobrar dos veces**) |
| `enrollment` | ✅ | `ConcederMatriculaPorPagoCapturado` |
| `learning` | ✅ | `EstudianteMatriculado` |
| `certification` | ✅ | `CursoFinalizado` |
| `course-authoring` | ❌ | no consume mensajes |

## 4. Transacción local de Enrollment (caso más exigente)

En **una sola transacción local** se confirman:

1. la **Matrícula** creada, o la determinación de que **ya existía**;
2. la entrada del **ledger por `PurchaseId`**;
3. el **Outbox del reply** de la Saga;
4. el **Outbox de `EstudianteMatriculado`**, **solo si la Matrícula fue creada**;
5. la entrada del **Inbox** del comando procesado.

## 5. Ledger de `PurchaseId` en Enrollment

**Registro de aplicación, no de dominio.** El agregado `Matrícula` **no lo referencia** y **no
incorpora `PurchaseId`**.

- **Clave:** `PurchaseId` (única).
- **Datos:** `PurchaseId`, `StudentId`, `CourseId`, resultado (`Created` | `AlreadyExisted`),
  origen (`ThisPurchase` | `Other`), fecha de procesamiento, `MessageId` inicial.
- **Por qué no basta el Inbox:** un reintento de la Saga llega con un **`MessageId` nuevo**; el Inbox
  no lo detecta, el ledger sí. **Son complementarios.**

**Garantías:**

| Situación | Resultado |
|---|---|
| Primer procesamiento, sin matrícula previa | `Created` · **se emite `EstudianteMatriculado`** |
| Reintento del mismo `PurchaseId` | devuelve el resultado confirmado · **re-envía el reply** · **no re-emite el evento** |
| Mismo `MessageId` reentregado | descartado por **Inbox** |
| Matrícula preexistente de otro origen | `AlreadyExisted` / `Origin = Other` → la Saga pasa a **ManualReview** |
| `PurchaseId` reutilizado con otra pareja | **`PurchaseIdConflict`** (rechazo funcional definitivo) + alerta |

## 6. Comportamiento ante duplicados por servicio

| Servicio | Ante duplicado |
|---|---|
| `paid-enrollment` | no-op; el estado de Saga ya avanzó; **no re-emite** |
| `payment-provider-sim` | devuelve el **reply almacenado**; **no repite la operación** |
| `enrollment` | devuelve el resultado del ledger; **no re-emite el evento** |
| `learning` | crear el progreso es **no-op**; el sellado es *once-only* |
| `certification` | devuelve el certificado existente |

## 7. Mensajes inválidos y *poison*

Errores **funcionales** (contrato desconocido, versión no soportada, payload malformado) →
**Dead Letter Queue directa, sin reintentar**, con alerta. Errores **transitorios** → reintentos con
backoff exponencial y, agotados, DLQ.
