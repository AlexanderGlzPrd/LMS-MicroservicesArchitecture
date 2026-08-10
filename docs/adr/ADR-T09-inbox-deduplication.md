# ADR-T09 — Inbox y deduplicación

## Estado
Aceptado — 2026-07-16

## Contexto
La entrega de mensajes es **at-least-once**: el mismo mensaje puede llegar más de una vez. Además,
un reintento de la Saga puede repetir la **misma intención de negocio** con un `MessageId` **nuevo**.

## Problema
¿Cómo se evita que una entrega repetida produzca un efecto de negocio repetido?

## Alternativas consideradas
- **Confiar en el broker**: no existe entrega exactamente-una-vez; descartado.
- **Solo idempotencia de negocio**: cubre la repetición de intención, pero no evita reprocesar el
  mismo mensaje ni reemitir eventos.
- **Solo Inbox por `MessageId`**: cubre la reentrega, pero **no** el reintento con identificador nuevo.
- **Inbox + idempotencia de negocio + restricciones de unicidad**: cobertura completa.

## Decisión
**Inbox por `MessageId`** en todos los consumidores: `paid-enrollment`, `payment-provider-sim`,
`enrollment`, `learning` y `certification`. `course-authoring` no consume mensajes y no lo necesita.

Se combina con **idempotencia de negocio por claves naturales** (`(StudentId, CourseId)`, referencia
de Finalización) y, en `enrollment`, con el **ledger por `PurchaseId`** descrito en
[T23](./ADR-T23-paid-enrollment-command.md).

**Terminología:** consumo **at-least-once** + transacción local + deduplicación + idempotencia =
**efecto de negocio effectively-once**. No se afirma entrega exactamente-una-vez.

## Justificación
Cada mecanismo cubre un nivel distinto: el Inbox cubre el transporte, las claves naturales cubren el
negocio y las restricciones de unicidad cubren las carreras de concurrencia.

## Consecuencias positivas
- Duplicados inocuos y sin reemisión de eventos.
- Comportamiento previsible y comprobable ante reentregas.

## Consecuencias negativas
- Tabla adicional por consumidor y necesidad de purga.

## Riesgos residuales
Crecimiento de las tablas de Inbox y ledger; requiere política de retención documentada.

## Relación con criterios académicos
Curso 2: idempotencia, resiliencia, consistencia eventual.

## Decisiones relacionadas
[T08](./ADR-T08-transactional-outbox.md) · [T19](./ADR-T19-resilience.md) · [T23](./ADR-T23-paid-enrollment-command.md)
