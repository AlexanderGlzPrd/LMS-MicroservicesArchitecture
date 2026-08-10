# ADR-T11 — API Composition en un BFF

## Estado
Aceptado — 2026-07-16

## Contexto
La vista “mis cursos en progreso” necesita datos de dos servicios: Learning aporta `CourseId`, estado
y porcentaje; Course Authoring aporta el título y el número de lecciones. La rúbrica exige
composición de información entre microservicios.

## Problema
¿Dónde se compone la información sin violar Database per Service ni ensuciar el Gateway?

## Alternativas consideradas
- **Joins entre bases**: prohibido; rompe el ownership.
- **Composición en el API Gateway**: mezcla routing con lógica de composición.
- **Composición en el cliente**: no evidencia composición en servidor.
- **Proyección materializada**: duplicaría el modelo de Authoring dentro de otro servicio.
- **BFF dedicado**: componente mínimo, aislado y demostrable.

## Decisión
Un **BFF de composición** expone la vista compuesta, consultando **en paralelo** a Learning y a
Course Authoring, con timeout y Circuit Breaker por dependencia.

**Contrato de degradación:**

| Situación | Respuesta |
|---|---|
| Todo disponible | **200** con `isPartial: false` |
| Authoring no responde (enriquecimiento) | **200** con `isPartial: true`, `warnings[]` y campos de curso nulos |
| Learning no responde (fuente esencial) | **503** con ProblemDetails y `Retry-After` |

## Justificación
Learning aporta el recurso esencial y Authoring solo enriquece: la ausencia del enriquecimiento no
invalida la respuesta, pero la ausencia del recurso esencial sí. No se usa `206`, reservado a
peticiones con rango.

## Consecuencias positivas
- Composición demostrable con un escenario de degradación real.
- Gateway libre de lógica de composición.

## Consecuencias negativas
- Un componente adicional que desplegar y observar.
- Los clientes deben interpretar `isPartial`.

## Riesgos residuales
Latencia acumulada si ambas dependencias responden lento; acotada por timeouts.

## Relación con criterios académicos
Curso 1: API Composition y resiliencia. Curso 2: diseño de APIs REST.

## Decisiones relacionadas
[T04](./ADR-T04-database-per-service.md) · [T14](./ADR-T14-yarp-gateway.md) · [T19](./ADR-T19-resilience.md)
