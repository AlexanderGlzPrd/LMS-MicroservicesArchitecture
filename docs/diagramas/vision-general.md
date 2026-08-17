# Diagrama general de arquitectura

**Propósito:** visión completa del sistema con tecnologías, límites y tipos de comunicación.
**Audiencia:** evaluadores y desarrolladores. **Criterio académico:** Curso 1 (diagrama general), Curso 3 (cloud-native).

Línea continua = **HTTP síncrono** · línea discontinua = **mensajería RabbitMQ**.

```mermaid
flowchart TB
    subgraph Users["Usuarios"]
        INS["Instructor"]
        STU["Estudiante"]
        THIRD["Tercero (verificador)"]
    end

    KC["Keycloak<br/>OAuth2 / OIDC / JWT"]
    GW["API Gateway (YARP)<br/>routing · validación JWT"]
    BFF["BFF Composition<br/>vistas compuestas"]

    subgraph MVP["MVP funcional (flujo gratuito)"]
        CA["course-authoring"]
        EN["enrollment"]
        LE["learning<br/>CORE"]
        CE["certification"]
        CADB[("PostgreSQL<br/>authoring")]
        ENDB[("PostgreSQL<br/>enrollment")]
        LEDB[("PostgreSQL<br/>learning<br/>write + read")]
        CEDB[("PostgreSQL<br/>certification")]
    end

    subgraph ACAD["Extensión académica (Saga de compra)"]
        PE["paid-enrollment<br/>orquestador"]
        PS["payment-provider-sim"]
        PEDB[("PostgreSQL<br/>purchase/saga")]
        PSDB[("PostgreSQL<br/>payments")]
    end

    MQ{{"RabbitMQ<br/>MassTransit"}}

    subgraph OBS["Observabilidad"]
        OTEL["OpenTelemetry"]
        PROM["Prometheus"]
        GRAF["Grafana"]
        JAE["Jaeger"]
    end

    INS --> GW
    STU --> GW
    THIRD --> GW
    GW -.->|valida token| KC

    GW --> CA
    GW --> EN
    GW --> LE
    GW --> CE
    GW --> BFF
    GW --> PE

    BFF --> LE
    BFF --> CA

    EN -->|¿publicado?| CA
    LE -->|LessonIds frescos<br/>en toda escritura| CA
    CE -->|título| CA
    CE -->|nombre vía ACL<br/>Admin API| KC
    PE -->|ConsultarAcceso| EN

    EN -.->|EstudianteMatriculado| MQ
    MQ -.-> LE
    LE -.->|CursoFinalizado| MQ
    MQ -.-> CE

    PE -.->|comandos de Saga| MQ
    MQ -.-> PS
    MQ -.-> EN
    PS -.->|replies| MQ
    EN -.->|reply| MQ
    MQ -.-> PE

    CA --- CADB
    EN --- ENDB
    LE --- LEDB
    CE --- CEDB
    PE --- PEDB
    PS --- PSDB

    CA -.-> OTEL
    EN -.-> OTEL
    LE -.-> OTEL
    CE -.-> OTEL
    PE -.-> OTEL
    OTEL --> PROM
    OTEL --> JAE
    PROM --> GRAF
```

## Notas

- **No se publican al broker:** `CursoPublicado`, `ContenidoPublicadoModificado`, `LecciónCompletada`,
  `CertificadoEmitido`, `PurchaseConfirmada`, `PurchaseCompensada` (sin consumidor obligatorio).
- **Solo dos Integration Events** gobiernan procesos en el MVP: `EstudianteMatriculado` y `CursoFinalizado`.
- El **flujo gratuito no depende** de la extensión académica.
- Cada servicio posee **su propia base lógica**; no hay acceso cruzado.
