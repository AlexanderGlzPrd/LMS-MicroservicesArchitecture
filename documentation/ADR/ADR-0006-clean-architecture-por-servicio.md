# ADR-0006 — Clean Architecture por servicio

## Estado
Aceptado — 2026-07-16

## Contexto
Cada servicio debe mantener separadas las capas de presentación, aplicación, dominio e
infraestructura, y el dominio no debe depender de detalles técnicos.

## Problema
¿Qué estructura interna adoptar en cada microservicio?

## Alternativas consideradas
- **Capas tradicionales**: el dominio acabaría dependiendo de la infraestructura.
- **Hexagonal (puertos y adaptadores)**: equivalente en el fondo; su vocabulario nombra la frontera,
  no las capas.
- **Onion**: equivalente.
- **Clean Architecture**: nombra directamente los proyectos que se van a crear
  (Api / Application / Domain / Infrastructure).

## Decisión
Clean Architecture en cada servicio, con cuatro proyectos más uno opcional de contratos:
`Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`. Dependencias:
`Api → Application → Domain`, `Infrastructure → Application/Domain`. **El dominio no depende de nada.**

Ubicación de elementos: Commands, Queries, handlers y puertos en `Application`; Aggregate Roots,
Value Objects, invariantes y Domain Events en `Domain`; repositorios, Outbox/Inbox, clientes ACL y
mensajería en `Infrastructure`; DTO, validaciones y manejo global de errores en `Api`.

## Justificación
Es la estructura más reconocible, expresa la inversión de dependencias en el propio grafo de
proyectos y permite probar el dominio sin infraestructura.

## Consecuencias positivas
- Dominio aislado y testeable.
- Separación de capas comprobable con las referencias de proyecto.
- Repository Pattern natural (puerto en Application, adaptador en Infrastructure).

## Consecuencias negativas
- Más proyectos por servicio.
- Mapeos adicionales entre capas.

## Riesgos residuales
Que los tipos de `Contracts` se filtren a Application o Domain; prohibido explícitamente en
[ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md).

## Decisiones relacionadas
[ADR-0004](./ADR-0004-servicios-desplegables-por-contexto.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) · [ADR-0025](./ADR-0025-building-blocks-tecnicos.md)
