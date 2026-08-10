# ADR-T08 — Transactional Outbox

## Estado
Aceptado — 2026-07-16

## Contexto
Un servicio que confirma un cambio local y después publica un mensaje puede fallar entre ambas
acciones (*dual write*): el hecho queda registrado pero nadie se entera, o se publica un mensaje de
un cambio que no llegó a confirmarse.

## Problema
¿Cómo se garantiza que un mensaje se publique si y solo si el cambio local fue confirmado?

## Alternativas consideradas
- **Publicación directa tras confirmar**: simple, pero pierde mensajes ante una caída.
- **Transacción distribuida**: descartada por diseño (un Aggregate Root por transacción).
- **Reconciliación periódica sin Outbox**: frágil y difícil de razonar.
- **Transactional Outbox**: el mensaje se escribe en la misma transacción local y se publica después.

## Decisión
**Outbox transaccional** en los servicios que publican mensajes: `paid-enrollment`,
`payment-provider-sim`, `enrollment` y `learning`.

**No se aplica** en `certification` (no publica mensajes externos) ni en `course-authoring` (sus
eventos de dominio no se publican mientras no tengan consumidor).

En `enrollment`, la transacción local confirma conjuntamente: la Matrícula o el resultado
`AlreadyExisted`, la entrada del ledger por `PurchaseId`, el Outbox del reply de la Saga, el Outbox de
`EstudianteMatriculado` **solo si la Matrícula fue creada**, y la entrada del Inbox del comando
procesado.

## Justificación
Es el único mecanismo compatible con “un Aggregate Root por transacción” que elimina la pérdida de
mensajes sin recurrir a transacciones distribuidas.

## Consecuencias positivas
- Ningún hecho confirmado se queda sin propagar.
- Publicación desacoplada de la disponibilidad del broker.

## Consecuencias negativas
- Tabla adicional y un despachador que debe vigilarse.
- Latencia adicional entre la confirmación y la publicación.

## Riesgos residuales
Un despachador detenido retrasa toda la propagación: debe monitorizarse con métricas y alertas.
Crecimiento de la tabla: requiere política de purga.

## Relación con criterios académicos
Curso 2: consistencia eventual, resiliencia, EDA fiable.

## Decisiones relacionadas
[T07](./ADR-T07-rabbitmq-masstransit.md) · [T09](./ADR-T09-inbox-deduplication.md) · [T23](./ADR-T23-paid-enrollment-command.md)
