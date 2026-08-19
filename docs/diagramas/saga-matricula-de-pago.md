# Saga — Compra de Acceso a Curso (extensión académica)

**Propósito:** documentar la máquina de estados, las reconciliaciones y las compensaciones.
**Criterio académico:** Curso 2 (Saga, estados, compensaciones, consistencia eventual).

> ⚠️ **Extensión académica.** El **flujo gratuito del LMS no es una Saga** y no depende de esta.
> La Matrícula es el **último paso irreversible** y **nunca se revierte**.

## Máquina de estados

```mermaid
stateDiagram-v2
    [*] --> Iniciada
    Iniciada --> VerificandoAcceso

    VerificandoAcceso --> AutorizandoPago: sin acceso previo
    VerificandoAcceso --> Rechazada: ya matriculado (AlreadyEnrolled)
    VerificandoAcceso --> Rechazada: Enrollment no verificable

    AutorizandoPago --> PagoAutorizado: autorizado
    AutorizandoPago --> Rechazada: declinado
    AutorizandoPago --> VerificandoResultadoAutorizacion: resultado desconocido

    VerificandoResultadoAutorizacion --> PagoAutorizado: autorización confirmada
    VerificandoResultadoAutorizacion --> Rechazada: no existe autorización
    VerificandoResultadoAutorizacion --> ManualReview: sigue indeterminado

    PagoAutorizado --> CapturandoPago

    CapturandoPago --> PagoCapturado: capturado
    CapturandoPago --> Compensando: fallo definitivo (anular)
    CapturandoPago --> VerificandoResultadoCaptura: resultado desconocido

    VerificandoResultadoCaptura --> PagoCapturado: captura confirmada
    VerificandoResultadoCaptura --> Compensando: no capturado (anular)
    VerificandoResultadoCaptura --> ManualReview: sigue indeterminado

    PagoCapturado --> ConcediendoMatricula

    ConcediendoMatricula --> MatriculaConcedida: Created o AlreadyExisted (mismo Purchase)
    ConcediendoMatricula --> Compensando: rechazo definitivo (reembolso)
    ConcediendoMatricula --> VerificandoResultadoMatricula: sin respuesta
    ConcediendoMatricula --> ManualReview: AlreadyExisted de otro origen

    VerificandoResultadoMatricula --> MatriculaConcedida: acceso verificado
    VerificandoResultadoMatricula --> Compensando: rechazo confirmado (reembolso)
    VerificandoResultadoMatricula --> ManualReview: sigue indeterminado

    MatriculaConcedida --> Confirmada
    Compensando --> Compensada: anulación o reembolso confirmados
    Compensando --> ManualReview: fallo de compensación

    ManualReview --> Confirmada: ResolveAsConfirmed (con evidencia)
    ManualReview --> Compensando: RetryCompensation
    ManualReview --> Compensada: ResolveAsCompensated (verificada)
    ManualReview --> Cerrada: CloseWithoutAutomaticAction

    Confirmada --> [*]
    Rechazada --> [*]
    Compensada --> [*]
    Cerrada --> [*]
```

## Mensajes (todos con consumidor real)

```mermaid
sequenceDiagram
    autonumber
    participant PE as paid-enrollment
    participant MQ as RabbitMQ
    participant PS as payment-provider-sim
    participant EN as enrollment

    PE->>EN: ConsultarAcceso (HTTP · pre-check)
    PE->>MQ: AutorizarPago
    MQ->>PS: AutorizarPago
    PS->>MQ: PagoAutorizado
    MQ->>PE: PagoAutorizado
    PE->>MQ: CapturarPago
    MQ->>PS: CapturarPago
    PS->>MQ: PagoCapturado
    MQ->>PE: PagoCapturado
    PE->>MQ: ConcederMatriculaPorPagoCapturado
    MQ->>EN: ConcederMatriculaPorPagoCapturado
    Note over EN: transacción local:<br/>Matrícula + ledger PurchaseId +<br/>Outbox reply + Outbox evento (solo si Created) + Inbox
    EN->>MQ: MatriculaConcedida (Created | AlreadyExisted)
    MQ->>PE: MatriculaConcedida
    Note over PE: → Confirmada
```

## Reconciliación de un resultado desconocido

El orquestador emitió una operación y no observó su respuesta. Eso **no es un fracaso**: se averigua.

```mermaid
sequenceDiagram
    autonumber
    participant PE as paid-enrollment
    participant MQ as RabbitMQ
    participant PS as payment-provider-sim
    participant EN as enrollment

    PE->>MQ: CapturarPago
    MQ->>PS: CapturarPago
    Note over PS: aplica y persiste la captura,<br/>pero su respuesta se pierde
    Note over PE: vence el paso → VerificandoResultadoCaptura

    PE->>MQ: ConsultarEstadoDePago
    MQ->>PS: ConsultarEstadoDePago
    Note over PS: consulta pura:<br/>no autoriza, no captura, no mueve dinero
    PS->>MQ: EstadoDePagoReportado (Captured + marcas reales)
    MQ->>PE: EstadoDePagoReportado
    Note over PE: 1. restaura evidencia (CapturedAt real)<br/>2. elige la transición con la evidencia ya actualizada<br/>→ PagoCapturado

    PE->>MQ: ConcederMatriculaPorPagoCapturado
    MQ->>EN: ConcederMatriculaPorPagoCapturado
    Note over PE: si tampoco llega respuesta,<br/>la concesión se reconcilia REENVIANDO el comando
    Note over EN: el ledger por PurchaseId devuelve el resultado<br/>almacenado y NO re-emite EstudianteMatriculado

    Note over PE: agotados los intentos sin respuesta concluyente<br/>→ ManualReview, sin compensar nada
```

**Por qué la asimetría.** El pago se reconcilia **preguntando**, porque un comando de captura
reenviado sería indistinguible de una orden de cobrar. La concesión se reconcilia **reenviando el
comando**, porque el ledger devuelve además el origen del acceso, y una consulta genérica de
matrícula diría «está matriculado» sin decir por qué compra.

**El orden importa y no es negociable.** La compensación se decide leyendo `CapturedAt`, así que la
evidencia se restaura **antes** de elegir la transición. Sin ese orden, una captura silenciosa
reconciliada avanzaría con `CapturedAt` vacío y un rechazo posterior dispararía una **anulación sobre
un pago ya cobrado** en vez del reembolso que corresponde. Por eso `EstadoDePagoReportado` transporta
las cuatro marcas temporales reales del proveedor, y por eso el instante del mensaje **nunca** sustituye
al instante del efecto.

## Reglas de la Saga

| Regla | Aplicación |
|---|---|
| Antes de pagar se verifica el acceso | un estudiante ya matriculado **no vuelve a pagar** |
| Antes de capturar se autoriza | `AutorizandoPago` → `CapturandoPago` |
| **Fallo antes de captura** | **anulación** de la autorización |
| **Fallo después de captura** | **reembolso** |
| **Resultado desconocido** | **se reconcilia antes de compensar** (tres estados de verificación) |
| **Nunca se reembolsa** mientras el resultado de la Matrícula sea desconocido | `VerificandoResultadoMatricula` |
| Una Matrícula válida **no se revierte** | es el último paso, sin compensación posterior |
| **`ManualReview` no es terminal** | espera resolución; **toda respuesta tardía se registra** |
| Acceso de otro origen | **`ManualReview`**, nunca `Confirmada` automática |

`ResolveAsConfirmed` **solo** es válido si: el pago fue capturado · no existe reembolso · el ledger
corresponde al mismo `PurchaseId` · la Matrícula fue creada por esa compra o es un reintento de ella.
