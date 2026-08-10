# ADR-T13 — Saga académica: Compra de Acceso a Curso

## Estado
Aceptado con riesgos residuales — 2026-07-16

## Contexto
Las rúbricas exigen una Saga con estados, transiciones y **compensaciones**. El flujo gratuito del
LMS (matricular → progresar → finalizar → certificar) es una **coreografía EDA** de hechos
**irreversibles**: no admite compensación legítima. Llamarlo Saga sería incorrecto.

## Problema
¿Cómo demostrar una Saga real sin inventar compensaciones falsas ni contaminar el dominio del LMS?

## Alternativas consideradas
- **Llamar Saga al flujo gratuito**: fraudulento; no hay compensaciones.
- **Saga de publicación con validación externa**: exigiría despublicar, capacidad inexistente;
  contamina Course Authoring.
- **Saga de emisión de certificado**: su compensación sería revocar un certificado legítimamente
  ganado; revierte un hecho irreversible.
- **Saga de matrícula pagada**: compensaciones legítimas (anulación de autorización y reembolso),
  ya anticipada por la clasificación de subdominios.
- **No implementar Saga**: pérdida de un criterio obligatorio.

## Decisión
Implementar la Saga **“Compra de Acceso a Curso”** como **extensión académica**, orquestada por
`paid-enrollment`, con `payment-provider-sim` y `enrollment` como participantes.

**Orden deliberado:** los pasos reversibles primero y el **irreversible al final**
(verificar acceso → autorizar → capturar → conceder Matrícula → confirmar).

**Estados:** `Iniciada`, `VerificandoAcceso`, `AutorizandoPago`, `VerificandoResultadoAutorizacion`,
`PagoAutorizado`, `CapturandoPago`, `VerificandoResultadoCaptura`, `PagoCapturado`,
`ConcediendoMatricula`, `VerificandoResultadoMatricula`, `MatriculaConcedida`, `Confirmada`,
`Rechazada`, `Compensando`, `Compensada`, `ManualReview`, `Cerrada`.

**Reglas:** antes de pagar se verifica el acceso · fallo **antes** de capturar → **anulación** ·
fallo **después** de capturar → **reembolso** · un resultado desconocido **se reconcilia antes de
compensar** · **nunca se reembolsa mientras el resultado de la Matrícula sea desconocido** · una
Matrícula válida **no se revierte** · **`ManualReview` no es terminal** · **toda respuesta tardía se
registra** · las resoluciones manuales requieren evidencia.

**Resoluciones operativas:** `ResolveAsConfirmed` (solo con pago capturado, sin reembolso, ledger del
mismo `PurchaseId` y Matrícula creada por esa compra o reintento de ella), `RetryCompensation`,
`ResolveAsCompensated` (compensación verificada), `CloseWithoutAutomaticAction`.
Acceso proveniente de otro origen → **`ManualReview`**, nunca `Confirmada` automática.

**Comunicación:** los pasos que modifican estado viajan por **RabbitMQ**; `ConsultarAcceso` usa HTTP.
`PurchaseConfirmada` y `PurchaseCompensada` **no se publican**: sin consumidor.

## Justificación
Es la única alternativa con compensaciones legítimas que no toca el dominio del LMS: Learning y
Certification no participan y el flujo gratuito no cambia.

## Consecuencias positivas
- Saga real, con compensación ejecutable y observable.
- Dominio del LMS intacto.
- Separación explícita entre MVP y extensión académica.

## Consecuencias negativas
- Dos componentes adicionales y una máquina de estados que mantener.
- Requiere una segunda vía de escritura en Enrollment ([T23](./ADR-T23-paid-enrollment-command.md)).

## Riesgos residuales
`ManualReview` exige intervención humana por diseño. Un pago sobre acceso preexistente (carrera)
termina en `ManualReview`; la política comercial de reembolso queda fuera del MVP.

## Relación con criterios académicos
Curso 2: Saga, estados, transiciones, compensaciones, consistencia eventual, resiliencia.

## Decisiones relacionadas
[T06](./ADR-T06-communication.md) · [T07](./ADR-T07-rabbitmq-masstransit.md) · [T08](./ADR-T08-transactional-outbox.md) · [T09](./ADR-T09-inbox-deduplication.md) · [T23](./ADR-T23-paid-enrollment-command.md)
