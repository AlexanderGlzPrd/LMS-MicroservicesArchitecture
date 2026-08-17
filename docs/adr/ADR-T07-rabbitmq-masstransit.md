# ADR-T07 — RabbitMQ con MassTransit

## Estado
Aceptado — 2026-07-16

## Contexto
Se necesita mensajería para dos Integration Events del MVP y para los comandos y respuestas de la
Saga de compra, con reintentos, Dead Letter Queue y trazabilidad.

## Problema
¿Qué tecnología de mensajería se adopta?

## Alternativas consideradas
- **Apache Kafka**: log particionado con reproducción; potente para streaming, pero operación pesada
  en local y Kubernetes, sin DLQ nativa y sobredimensionado para el volumen del MVP.
- **RabbitMQ**: exchanges y colas con enrutamiento rico, DLQ y reintentos nativos, ligero en
  contenedor, ecosistema .NET maduro.
- **Otras alternativas**: no aportan ventaja real en este contexto.

## Decisión
**RabbitMQ** como broker y **MassTransit** como abstracción de mensajería en .NET.

Topología conceptual: exchanges por contexto productor (`lms.enrollment`, `lms.learning`,
`lms.saga.commands`, `lms.saga.replies`), una cola dedicada por consumidor y una cola de mensajes
muertos asociada a cada una.

## Justificación
Cubre exactamente lo necesario (enrutamiento, reintentos, DLQ) con la menor complejidad operativa, y
permite demostrar los conceptos exigidos sin la carga de un sistema de streaming.

## Consecuencias positivas
- DLQ y políticas de reintento nativas.
- Ejecución local y en Kubernetes sencilla.
- Idempotencia apoyada en identificadores de mensaje.

## Consecuencias negativas
- Sin reproducción histórica de mensajes (no requerida).
- Una abstracción adicional que conviene comprender y documentar.

## Riesgos residuales
Dependencia de una capa de abstracción: se documentará explícitamente cómo funciona el Outbox para
evidenciar comprensión y no solo uso.

## Relación con criterios académicos
Curso 2: EDA, broker, productores y consumidores, contratos de Integration Events, resiliencia.

## Decisiones relacionadas
[T06](./ADR-T06-comunicacion.md) · [T08](./ADR-T08-outbox-transaccional.md) · [T09](./ADR-T09-inbox-deduplicacion.md) · [T13](./ADR-T13-saga-matricula-de-pago.md)
