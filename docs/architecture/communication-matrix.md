# Matriz de comunicación

- **Fecha:** 2026-07-16 · **Estado:** congelado · Decisión: [ADR-T06](../adr/ADR-T06-communication.md)

---

## 1. Principio

- **HTTP síncrono** → **consultas de verificación** que exigen frescura inmediata.
- **RabbitMQ asíncrono** → **hechos de negocio con consumidor obligatorio** y **pasos de la Saga que
  modifican estado**.
- **No se publica ningún mensaje sin consumidor real.** Logs, métricas y trazas **no** requieren
  Integration Events.

## 2. Comunicación síncrona (HTTP)

| Origen → Destino | Qué consulta | Si no está disponible |
|---|---|---|
| Enrollment → Course Authoring | ¿el curso está publicado? | **fail-safe**: no se matricula |
| **Learning → Course Authoring** | **conjunto actual de `LessonIds`, en toda escritura** | **503**, no se modifica el agregado ni se sella |
| Certification → Course Authoring | título del curso (para congelarlo) | no se emite; reintento posterior |
| Certification → Keycloak Admin API (vía ACL) | nombre del estudiante (para congelarlo) | no se emite; reintento posterior |
| BFF → Learning y, después, → Course Authoring | vista compuesta: Learning determina los `CourseId` del estudiante y, conocidos estos, los enriquecimientos independientes a Authoring se ejecutan de forma concurrente y acotada | degradación controlada (§6) |
| paid-enrollment → Enrollment | `ConsultarAcceso` (pre-check antes de pagar) | reintentos; luego `Rechazada(PreCheckUnavailable)` |

Todas con **timeout acotado, reintentos con backoff y Circuit Breaker**.

> **Aclaración de causalidad — 2026-08-17.** La fila del BFF decía «vista compuesta (en paralelo)».
> Se precisa que la concurrencia corresponde a los enriquecimientos hacia Authoring, no a un arranque
> simultáneo de las dos fuentes: Authoring se consulta por `CourseId` y esos identificadores solo los
> conoce Learning. Aclaración de redacción alineada con [ADR-T11](../adr/ADR-T11-api-composition.md);
> **no cambia ninguna decisión** y el ADR sigue Aceptado. Se corrige de paso la referencia cruzada de
> la fila, que apuntaba a §5 en vez de a §6.

## 3. Comunicación asíncrona — Integration Events (MVP)

| Evento | Productor | Consumidor | Información mínima |
|---|---|---|---|
| `EstudianteMatriculado` | Enrollment | **Learning** | `StudentId`, `CourseId` |
| `CursoFinalizado` | Learning | **Certification** | `StudentId`, `CourseId`, fecha de finalización |

**Solo estos dos** gobiernan procesos de aplicación en el MVP.

## 4. Comunicación asíncrona — mensajes de Saga (extensión académica)

| Mensaje | Productor → Consumidor |
|---|---|
| `AutorizarPago` / `PagoAutorizado`·`PagoDeclinado` | paid-enrollment ↔ payment-provider-sim |
| `CapturarPago` / `PagoCapturado`·`CapturaFallida` | paid-enrollment ↔ payment-provider-sim |
| `AnularAutorizacion` / `AutorizacionAnulada` | paid-enrollment ↔ payment-provider-sim |
| `ReembolsarPago` / `PagoReembolsado`·`ReembolsoFallido` | paid-enrollment ↔ payment-provider-sim |
| **`ConcederMatriculaPorPagoCapturado`** / `MatriculaConcedida`·`MatriculaRechazada` | paid-enrollment ↔ **enrollment** |

**Todos tienen consumidor real.**

## 5. Eventos de dominio que NO se publican

| Evento | Motivo |
|---|---|
| `CursoPublicado` | sin consumidor obligatorio; Learning obtiene el conjunto **fresco al operar** |
| `ContenidoPublicadoModificado` | ídem |
| `LecciónCompletada` | **interno** de Learning; alimenta su modelo de lectura |
| `CertificadoEmitido` | sin consumidor downstream en el MVP |
| `PurchaseConfirmada` / `PurchaseCompensada` | sin consumidor; los estados viven en logs, métricas y trazas |

## 6. Degradación de la composición (BFF)

| Situación | Respuesta |
|---|---|
| Todo disponible | **200** · `isPartial: false` |
| **Authoring no responde** (enriquecimiento) | **200** · `isPartial: true` · `warnings[]` · título y nº de lecciones nulos |
| **Learning no responde** (fuente esencial) | **503** · ProblemDetails · `Retry-After` |

## 7. Resumen de disponibilidad

| Si cae… | Consecuencia |
|---|---|
| Course Authoring | no se puede matricular, **ni marcar lecciones**, ni emitir certificados (todo fail-safe) |
| Enrollment | no se conceden accesos nuevos; el progreso existente sigue funcionando |
| Learning | no hay progreso ni finalización; el BFF responde 503 |
| Certification | los certificados se emiten al restablecerse; la Finalización ya está sellada |
| RabbitMQ | los hechos quedan en Outbox y se publican al restablecerse |
