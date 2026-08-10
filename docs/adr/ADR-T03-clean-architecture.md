# ADR-T03 — Clean Architecture por servicio

## Estado
Aceptado — 2026-07-16

## Contexto
Cada servicio debe mantener separadas las capas de presentación, aplicación, dominio e
infraestructura, y el dominio no debe depender de detalles técnicos.

## Problema
¿Qué estructura interna adoptar en cada microservicio?

## Alternativas consideradas
- **Capas tradicionales**: el dominio acabaría dependiendo de la infraestructura.
- **Hexagonal (puertos y adaptadores)**: equivalente en el fondo; vocabulario menos alineado con la rúbrica.
- **Onion**: equivalente.
- **Clean Architecture**: vocabulario alineado con la rúbrica (Api / Application / Domain / Infrastructure).

## Decisión
Clean Architecture en cada servicio, con cuatro proyectos más uno opcional de contratos:
`Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`. Dependencias:
`Api → Application → Domain`, `Infrastructure → Application/Domain`. **El dominio no depende de nada.**

Ubicación de elementos: Commands, Queries, handlers y puertos en `Application`; Aggregate Roots,
Value Objects, invariantes y Domain Events en `Domain`; repositorios, Outbox/Inbox, clientes ACL y
mensajería en `Infrastructure`; DTO, validaciones y manejo global de errores en `Api`.

## Justificación
Es la estructura más reconocible y enseñable, expresa la inversión de dependencias y permite probar
el dominio sin infraestructura.

## Consecuencias positivas
- Dominio aislado y testeable.
- Fronteras claras para evaluar “separación de capas”.
- Repository Pattern natural (puerto en Application, adaptador en Infrastructure).

## Consecuencias negativas
- Más proyectos por servicio.
- Mapeos adicionales entre capas.

## Riesgos residuales
Que los tipos de `Contracts` se filtren a Application o Domain; prohibido explícitamente en
[T20](./ADR-T20-contract-versioning.md).

## Relación con criterios académicos
Curso 2: buenas prácticas .NET, separación de capas, DDD. Curso 1: Repository Pattern.

## Decisiones relacionadas
[T01](./ADR-T01-physical-microservices.md) · [T20](./ADR-T20-contract-versioning.md) · [T22](./ADR-T22-technical-building-blocks.md)
