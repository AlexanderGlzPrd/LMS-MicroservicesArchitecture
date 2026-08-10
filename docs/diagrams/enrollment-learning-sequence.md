# Flujo Enrollment → Learning

**Propósito:** mostrar la coreografía EDA del flujo gratuito con transacción local, Outbox, broker,
Inbox y comportamiento ante duplicados.
**Criterio académico:** Curso 2 (EDA, consistencia eventual, idempotencia).

> Este flujo es **coreografía EDA**, **no una Saga**: sus hechos son irreversibles y no admiten
> compensación legítima.

```mermaid
sequenceDiagram
    autonumber
    actor STU as Estudiante
    participant GW as API Gateway
    participant EN as enrollment
    participant CA as course-authoring
    participant MQ as RabbitMQ
    participant LE as learning

    STU->>GW: POST /enrollments (JWT)
    GW->>GW: valida firma, issuer, audiencia, expiración
    GW->>EN: matricular (StudentId = claim sub)
    EN->>EN: revalida el JWT

    EN->>CA: GET ¿curso publicado? (HTTP)
    alt Authoring no disponible
        CA--xEN: timeout / Circuit Breaker abierto
        EN-->>STU: 503 — fail-safe, no se matricula
    else Curso publicado
        CA-->>EN: publicado = true

        rect rgb(235, 245, 255)
            note over EN: TRANSACCIÓN LOCAL
            EN->>EN: verifica unicidad (StudentId, CourseId)
            EN->>EN: crea Matrícula
            EN->>EN: escribe Outbox: EstudianteMatriculado
        end

        EN-->>STU: 201 acceso concedido
        EN->>MQ: publica EstudianteMatriculado (tras confirmar)
    end

    MQ->>LE: entrega EstudianteMatriculado (at-least-once)
    LE->>LE: Inbox — ¿MessageId ya procesado?

    alt Primera entrega
        rect rgb(235, 255, 240)
            note over LE: TRANSACCIÓN LOCAL
            LE->>LE: crea ProgresoDelCurso (EnProgreso, 0 completadas)
            LE->>LE: registra MessageId en Inbox
        end
    else Reentrega o duplicado
        LE->>LE: NO-OP — el progreso ya existe por (StudentId, CourseId)
    end
```

## Garantías

| Aspecto | Mecanismo |
|---|---|
| Entrega | **at-least-once** (el broker puede repetir) |
| Deduplicación de transporte | **Inbox por `MessageId`** |
| Idempotencia de negocio | clave natural **`(StudentId, CourseId)`** |
| Efecto observable | **effectively-once** |
| Fallo de Learning | el acceso **ya está concedido**; nada se revierte; se reintenta la entrega |
| Ventana temporal | el estudiante tiene acceso pero aún no puede marcar lecciones (**riesgo aceptado nº 1**) |
