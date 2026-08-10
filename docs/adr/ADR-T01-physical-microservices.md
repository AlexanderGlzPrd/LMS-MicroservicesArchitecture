# ADR-T01 — Microservicios físicos independientes

## Estado
Aceptado — 2026-07-16

## Contexto
El Strategic Design definió cuatro Bounded Contexts (Course Authoring, Enrollment, Learning,
Certification) y una extensión académica de compra. Las rúbricas exigen servicios desplegables de
forma independiente y un mínimo de tres microservicios.

## Problema
¿Los Bounded Contexts se materializan como unidades físicas independientes o como módulos de un
único despliegue?

## Alternativas consideradas
- **Monolito modular**: menor complejidad operativa, pero no cumple la separación física exigida.
- **Un servicio por Bounded Context**: autonomía real de desarrollo, despliegue y persistencia.
- **Agrupar contextos** (p. ej. Enrollment + Learning): reintroduciría la doble autoridad que el
  Strategic Design descartó.

## Decisión
Seis servicios de aplicación desplegables de forma independiente: `course-authoring`, `enrollment`,
`learning`, `certification` (MVP) más `paid-enrollment` y `payment-provider-sim` (extensión
académica), junto a `gateway` y `bff-composition` como componentes técnicos.

## Justificación
Cada Bounded Context posee sus propias invariantes, su propia autoridad sobre sus conceptos y su
propio ciclo de cambio. La separación física preserva esa autonomía y satisface la exigencia
académica de despliegue independiente.

## Consecuencias positivas
- Autonomía de desarrollo, despliegue, persistencia y escalado.
- Fronteras de ownership visibles y verificables.
- Cumple el mínimo de microservicios de los tres cursos.

## Consecuencias negativas
- Complejidad operativa y de depuración distribuida.
- Consistencia eventual entre servicios.
- Mayor coste de recursos en local.

## Riesgos residuales
Alcance ambicioso para un primer proyecto de microservicios; se mitiga con la estrategia de
incrementos verticales (los incrementos 1–6 ya entregan un sistema funcional).

## Relación con criterios académicos
Curso 1: *Decompose by Business Capability*. Curso 2 y 3: mínimo de tres microservicios,
arquitectura desacoplada.

## Decisiones relacionadas
[ADR-0001](./0001-microservicios-como-driver-de-aprendizaje.md) · [ADR-0003](./0003-clasificacion-subdominios.md) · [T02](./ADR-T02-monorepo.md) · [T03](./ADR-T03-clean-architecture.md) · [T04](./ADR-T04-database-per-service.md)
