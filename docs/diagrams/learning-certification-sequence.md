# Flujo Learning → Certification

**Propósito:** mostrar el conjunto fresco de `LessonIds`, el sellado de la Finalización, la
propagación y la emisión del certificado con reintento si falta información.
**Criterio académico:** Curso 2 (EDA, idempotencia), Curso 1 (resiliencia).

```mermaid
sequenceDiagram
    autonumber
    actor STU as Estudiante
    participant LE as learning
    participant CA as course-authoring
    participant MQ as RabbitMQ
    participant CE as certification
    participant KC as Keycloak (Admin API vía ACL)

    STU->>LE: PUT /progress/{courseId}/lessons/{lessonId}

    LE->>CA: GET conjunto ACTUAL de LessonIds (fresco, obligatorio)
    alt Authoring no disponible
        CA--xLE: timeout / Circuit Breaker abierto
        LE-->>STU: 503 — no se modifica el agregado, no se sella
    else Conjunto obtenido
        CA-->>LE: LessonIds actuales

        rect rgb(235, 255, 240)
            note over LE: TRANSACCIÓN LOCAL
            LE->>LE: valida pertenencia de la LessonId
            LE->>LE: añade a completadas
            LE->>LE: deriva 100% (condición, no estado)
            alt Criterio cumplido y observado por esta acción
                LE->>LE: SELLA Finalización (inmutable): Status y CompletedAt
                LE->>LE: escribe Outbox: CursoFinalizado
            else Aún no
                LE->>LE: permanece EnProgreso
            end
        end

        LE-->>STU: 200 (marcada · posiblemente finalizada)
        LE->>MQ: publica CursoFinalizado (una sola vez)
    end

    MQ->>CE: entrega CursoFinalizado (at-least-once)

    note over CE: FASE 1 — aceptar el hecho
    CE->>CE: Inbox — ¿MessageId procesado?

    alt Ya procesado, o ya existe certificado para esta Finalización
        CE->>CE: NO-OP idempotente · ACK
    else Aceptación
        rect rgb(255, 248, 235)
            note over CE: TRANSACCIÓN LOCAL
            CE->>CE: registra Inbox (MessageId)
            CE->>CE: registra Emisión Pendiente (StudentId, CourseId, CompletedAt)
        end
        CE->>CE: ACK — todavía NO hay certificado
    end

    note over CE: FASE 2 — emitir (proceso posterior, reintentable)
    CE->>CA: GET título del curso
    CE->>KC: GET nombre visible del estudiante (vía IStudentDirectory)

    alt Falta alguna fuente
        CE->>CE: NO emite — nada parcial · la pendiente sobrevive · reintento posterior
    else Información completa
        rect rgb(235, 255, 240)
            note over CE: TRANSACCIÓN LOCAL
            CE->>CE: congela StudentSnapshot y CourseSnapshot
            CE->>CE: crea Certificado inmutable (emisor = plataforma)
            CE->>CE: cierra la Emisión Pendiente
        end
    end
```

## Reglas visibles

- **`LessonIds` frescos en toda escritura**: la caché solo se usa en consultas y se marca como aproximada.
- **El sellado no persiste ningún snapshot de `LessonIds`**: sella `Status` y `CompletedAt`, y nada
  más (ver [ADR-T27](../adr/ADR-T27-no-lesson-set-snapshot.md)). El conjunto publicado pertenece a
  Authoring y no forma parte del estado del agregado.
- La **Finalización es inmutable** y `CursoFinalizado` se produce **una sola vez**.
- **Aceptar el hecho y emitir el certificado son dos fases distintas.** El Inbox afirma que el evento
  fue aceptado y convertido en trabajo interno durable, **no** que el certificado exista. Eso permite
  confirmar el mensaje sin emitir, sin perder el hecho y sin bloquear la cola mientras una fuente
  esté caída.
- El certificado **nace solo con información completa**; la fecha procede de la Finalización y el
  nombre y el título se **congelan al emitir**. Si falta alguna fuente, la **emisión pendiente
  sobrevive** y se reintenta después, sin intervención manual.
- El nombre visible se obtiene siempre a través del puerto `IStudentDirectory`. Su fuente definitiva
  es Keycloak Admin API vía ACL ([ADR-T15](../adr/ADR-T15-keycloak-security.md)); hasta ese
  incremento lo satisface un adaptador **provisional**
  ([ADR-T26](../adr/ADR-T26-provisional-student-directory.md)).
- **`CertificadoEmitido` no se publica**: no tiene consumidor en el MVP.
- Si la propagación falla, **la Finalización no se revierte**: se reintenta la entrega.
