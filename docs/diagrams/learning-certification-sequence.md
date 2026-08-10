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
                LE->>LE: SELLA Finalización (inmutable) + snapshot de LessonIds
                LE->>LE: escribe Outbox: CursoFinalizado
            else Aún no
                LE->>LE: permanece EnProgreso
            end
        end

        LE-->>STU: 200 (marcada · posiblemente finalizada)
        LE->>MQ: publica CursoFinalizado (una sola vez)
    end

    MQ->>CE: entrega CursoFinalizado (at-least-once)
    CE->>CE: Inbox — ¿MessageId procesado?
    CE->>CE: ledger — ¿existe certificado para esta Finalización?

    alt Ya existe
        CE->>CE: NO-OP idempotente
    else Emisión
        CE->>CA: GET título del curso
        CE->>KC: GET nombre visible del estudiante
        alt Falta alguna fuente
            CE->>CE: NO emite — nada parcial · reintento posterior
        else Información completa
            rect rgb(255, 248, 235)
                note over CE: TRANSACCIÓN LOCAL
                CE->>CE: congela StudentSnapshot y CourseSnapshot
                CE->>CE: crea Certificado inmutable (emisor = plataforma)
            end
        end
    end
```

## Reglas visibles

- **`LessonIds` frescos en toda escritura**: la caché solo se usa en consultas y se marca como aproximada.
- La **Finalización es inmutable** y `CursoFinalizado` se produce **una sola vez**.
- El certificado **nace solo con información completa**; la fecha procede de la Finalización y el
  nombre y el título se **congelan al emitir**.
- **`CertificadoEmitido` no se publica**: no tiene consumidor en el MVP.
- Si la propagación falla, **la Finalización no se revierte**: se reintenta la entrega.
