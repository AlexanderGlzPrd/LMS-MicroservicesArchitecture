# ADR-0008 — PostgreSQL con EF Core

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
- **Motores distintos por servicio**: posible, pero ningún contexto tiene un patrón de acceso que lo
  justifique, y multiplica el coste de operación.

## Decisión
**PostgreSQL** como único motor, con **EF Core** como acceso a datos y migraciones por servicio.

## Justificación
Cubre todas las necesidades identificadas con el menor coste operativo en local y en Kubernetes. La
homogeneidad es deliberada: la complejidad del sistema está en su arquitectura distribuida, no en la
variedad de motores.

## Consecuencias positivas
- Un solo motor que operar, respaldar y desplegar.
- Restricciones de unicidad que protegen invariantes de conjunto.
- Migraciones independientes por servicio.

## Consecuencias negativas
- Homogeneidad tecnológica: no se demuestra poliglotismo de persistencia (no exigido).

## Riesgos residuales
Crecimiento de las tablas de Outbox, Inbox y ledger: requiere una política de purga documentada
antes de considerar el sistema operable a largo plazo.

## Decisiones relacionadas
[ADR-0007](./ADR-0007-base-de-datos-por-servicio.md) · [ADR-0011](./ADR-0011-outbox-transaccional.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md)
