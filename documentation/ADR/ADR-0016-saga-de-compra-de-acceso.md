# ADR-0016 — Saga de compra de acceso a curso

## Estado
Aceptado con riesgos residuales — 2026-07-16

## Contexto
La plataforma incorpora cursos de pago: el acceso deja de concederse por solicitud del estudiante y
pasa a depender de un cobro que ocurre en un proveedor externo. Ese proceso cruza tres servicios,
dura más que una petición HTTP y puede fallar en cualquier punto **después** de haber movido dinero.

El flujo gratuito (matricular → progresar → finalizar → certificar) no tiene ese problema: es una
coreografía de hechos **irreversibles** que no admite compensación legítima.

## Problema
¿Cómo se coordina la compra de acceso de forma que un fallo intermedio no deje al estudiante pagado
y sin acceso, ni con acceso y sin pago, y sin contaminar el dominio del LMS?

## Alternativas consideradas
- **Tratar la compra como una transacción distribuida**: descartada por diseño; el proveedor de
  pagos no participa en ninguna transacción propia.
- **Coordinar la compra dentro de Enrollment**: convierte al contexto que concede acceso en
  responsable de precios, cobros y reembolsos, que no son suyos.
- **Coreografía sin orquestador**: nadie conoce el estado global de la compra y las compensaciones
  quedan repartidas entre servicios que no saben en qué punto falló el proceso.
- **Saga orquestada en un servicio propio**: compensaciones legítimas (anulación de autorización y
  reembolso), ya anticipada por la clasificación de subdominios, que separa la **orquestación de
  pagos** (Supporting) del **procesamiento** (Generic).

## Decisión
Implementar la Saga **“Compra de Acceso a Curso”** orquestada por `paid-enrollment`, con
`payment-provider-sim` y `enrollment` como participantes.

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
El orquestador aísla en un solo componente lo único que la compra añade —dinero, autorización,
captura, reembolso— y deja intacto el resto: Learning y Certification no participan y el flujo
gratuito no cambia. Ordenar los pasos con el irreversible al final reduce la compensación a
operaciones que el proveedor de pagos sí admite.

## Consecuencias positivas
- Estado global de la compra en un único lugar, observable y compensable.
- Dominio del LMS intacto.
- Frontera explícita entre el flujo gratuito y el de pago.

## Consecuencias negativas
- Dos componentes adicionales y una máquina de estados que mantener.
- Requiere una segunda vía de escritura en Enrollment ([ADR-0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md)).

## Riesgos residuales
`ManualReview` exige intervención humana por diseño. Un pago sobre acceso preexistente (carrera)
termina en `ManualReview`; la política comercial de reembolso queda fuera del MVP.

## Decisiones relacionadas
[ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0010](./ADR-0010-rabbitmq-y-masstransit.md) · [ADR-0011](./ADR-0011-outbox-transaccional.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md) · [ADR-0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md)
