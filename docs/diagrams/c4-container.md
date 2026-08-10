# C4 — Nivel 2: Contenedores

**Propósito:** unidades desplegables, sus tecnologías y persistencias.
**Audiencia:** desarrolladores y evaluadores. **Criterio académico:** Curso 1 (arquitectura), Curso 3 (cloud-native).

```mermaid
flowchart TB
    subgraph Edge["Borde"]
        GW["API Gateway<br/><i>YARP</i>"]
        BFF["BFF Composition<br/><i>ASP.NET Core</i>"]
    end

    subgraph Domain["Servicios de dominio (MVP)"]
        CA["course-authoring<br/><i>ASP.NET Core · EF Core</i>"]
        EN["enrollment<br/><i>ASP.NET Core · EF Core</i>"]
        LE["learning (Core)<br/><i>ASP.NET Core · EF Core · CQRS</i>"]
        CE["certification<br/><i>ASP.NET Core · EF Core</i>"]
    end

    subgraph Academic["Extensión académica"]
        PE["paid-enrollment<br/><i>orquestador de Saga</i>"]
        PS["payment-provider-sim<br/><i>proveedor simulado</i>"]
    end

    subgraph Data["Persistencia — una base lógica por servicio"]
        D1[("authoring_db")]
        D2[("enrollment_db")]
        D3[("learning_db")]
        D4[("certification_db")]
        D5[("purchase_db")]
        D6[("payments_db")]
    end

    MQ{{"RabbitMQ<br/><i>MassTransit</i>"}}
    KC["Keycloak"]

    GW --> CA & EN & LE & CE & PE & BFF
    BFF --> LE & CA
    EN --> CA
    LE --> CA
    CE --> CA
    CE --> KC
    PE --> EN

    EN -.-> MQ -.-> LE
    LE -.-> MQ -.-> CE
    PE -.-> MQ -.-> PS
    PE -.-> MQ -.-> EN

    CA --- D1
    EN --- D2
    LE --- D3
    CE --- D4
    PE --- D5
    PS --- D6
```

## Estructura interna de cada servicio (Clean Architecture)

```mermaid
flowchart LR
    API["Api<br/>controllers · DTO · validación"] --> APP["Application<br/>Commands · Queries · puertos"]
    APP --> DOM["Domain<br/>Aggregates · VO · eventos<br/><b>sin dependencias</b>"]
    INF["Infrastructure<br/>EF Core · Outbox/Inbox · ACL"] --> APP
    INF --> DOM
    CON["Contracts<br/>mensajes publicados"] -.->|usado solo en Infrastructure| INF
```

## Notas

- Ningún servicio accede a la base de otro. **No hay tablas compartidas ni joins entre servicios.**
- Un tipo de `Contracts` **no puede salir de Infrastructure/ACL**.
- Building blocks compartidos: solo técnicos (mensajería, observabilidad, web, testing).
