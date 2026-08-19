# ADR-0004 — Un servicio desplegable por Bounded Context

## Estado
Aceptado — 2026-07-16

## Contexto
El diseño estratégico definió cuatro Bounded Contexts (Course Authoring, Enrollment, Learning,
Certification) y un flujo adicional de compra de acceso. Cada uno tiene su propia autoridad sobre
sus conceptos y su propio ritmo de cambio.

## Problema
¿Los Bounded Contexts se materializan como unidades físicas independientes o como módulos de un
único despliegue?

## Alternativas consideradas
- **Monolito modular**: menor complejidad operativa, pero las fronteras dependen de la disciplina y
  el despliegue queda acoplado.
- **Un servicio por Bounded Context**: autonomía real de desarrollo, despliegue y persistencia.
- **Agrupar contextos** (p. ej. Enrollment + Learning): reintroduciría la doble autoridad que el
  diseño estratégico descartó.

## Decisión
Seis servicios de aplicación desplegables de forma independiente: `course-authoring`, `enrollment`,
`learning`, `certification` (MVP) más `paid-enrollment` y `payment-provider-sim` (compra de acceso),
junto a `gateway` y `bff-composition` como componentes técnicos.

## Justificación
Cada Bounded Context posee sus propias invariantes, su propia autoridad sobre sus conceptos y su
propio ciclo de cambio. La separación física preserva esa autonomía y hace que las fronteras se
puedan verificar en lugar de acordarse.

## Consecuencias positivas
- Autonomía de desarrollo, despliegue, persistencia y escalado.
- Fronteras de ownership visibles y verificables.

## Consecuencias negativas
- Complejidad operativa y de depuración distribuida.
- Consistencia eventual entre servicios.
- Mayor coste de recursos en local.

## Riesgos residuales
Alcance amplio para una sola línea de desarrollo; se mitiga construyendo por cortes verticales, de
modo que el sistema es funcional de extremo a extremo mucho antes de estar completo.

## Decisiones relacionadas
[ADR-0001](./ADR-0001-estilo-arquitectonico-microservicios.md) · [ADR-0003](./ADR-0003-clasificacion-de-subdominios.md) · [ADR-0005](./ADR-0005-monorepo.md) · [ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) · [ADR-0007](./ADR-0007-base-de-datos-por-servicio.md)
