# ADR-0010 — RabbitMQ con MassTransit

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
Cubre exactamente lo necesario (enrutamiento, reintentos, DLQ) con la menor complejidad operativa,
sin la carga de operar un sistema de streaming que el volumen no justifica.

## Consecuencias positivas
- DLQ y políticas de reintento nativas.
- Ejecución local y en Kubernetes sencilla.
- Idempotencia apoyada en identificadores de mensaje.

## Consecuencias negativas
- Sin reproducción histórica de mensajes (no requerida).
- Una abstracción adicional entre el código y el broker.

## Riesgos residuales
Dependencia de una capa de abstracción: el comportamiento del Outbox se documenta de forma explícita
para que el diagnóstico no dependa de conocer los detalles internos de MassTransit.

## Decisiones relacionadas
[ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0011](./ADR-0011-outbox-transaccional.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md) · [ADR-0016](./ADR-0016-saga-de-compra-de-acceso.md)
