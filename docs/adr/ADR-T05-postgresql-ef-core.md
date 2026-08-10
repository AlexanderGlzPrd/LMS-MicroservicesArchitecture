# ADR-T05 — PostgreSQL con EF Core

## Estado
Aceptado — 2026-07-16

## Contexto
Los servicios necesitan transacciones locales, restricciones de unicidad para proteger invariantes
de conjunto y almacenamiento para Outbox, Inbox, ledger y modelos de lectura.

## Problema
¿Qué motor de persistencia y qué acceso a datos se adoptan?

## Alternativas consideradas
- **SQL Server**: muy familiar en .NET; imagen más pesada en contenedor.
- **PostgreSQL**: ligero en contenedor y Kubernetes, soporte maduro en EF Core, restricciones y tipos
  adecuados para proyecciones.
- **Motores distintos por servicio**: posible en teoría, pero sin necesidad real y con coste de
  aprendizaje y operación innecesario.

## Decisión
**PostgreSQL** como único motor, con **EF Core** como acceso a datos y migraciones por servicio.

## Justificación
Cubre todas las necesidades identificadas con el menor coste operativo en local y en Kubernetes, y
mantiene el foco del aprendizaje en la arquitectura y no en la diversidad tecnológica.

## Consecuencias positivas
- Un solo motor que aprender, operar y desplegar.
- Restricciones de unicidad que protegen invariantes de conjunto.
- Migraciones independientes por servicio.

## Consecuencias negativas
- Homogeneidad tecnológica: no se demuestra poliglotismo de persistencia (no exigido).

## Riesgos residuales
Crecimiento de las tablas de Outbox, Inbox y ledger: requiere una política de purga documentada
antes de considerar el sistema operable a largo plazo.

## Relación con criterios académicos
Curso 1: *Database per Service*, Repository Pattern. Curso 2: idempotencia y consistencia eventual.

## Decisiones relacionadas
[T04](./ADR-T04-database-per-service.md) · [T08](./ADR-T08-transactional-outbox.md) · [T09](./ADR-T09-inbox-deduplication.md)
