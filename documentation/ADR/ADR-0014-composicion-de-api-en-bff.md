# ADR-0014 — API Composition en un BFF

## Estado
Aceptado — 2026-07-16

## Contexto
La vista “mis cursos en progreso” necesita datos de dos servicios: Learning aporta `CourseId`, estado
y porcentaje; Course Authoring aporta el título y el número de lecciones. Ninguno de los dos puede
responderla solo.

## Problema
¿Dónde se compone la información sin violar Database per Service ni ensuciar el Gateway?

## Alternativas consideradas
- **Joins entre bases**: prohibido; rompe el ownership.
- **Composición en el API Gateway**: mezcla routing con lógica de composición.
- **Composición en el cliente**: traslada el conocimiento de la topología de servicios a cada
  consumidor y multiplica las llamadas desde fuera del sistema.
- **Proyección materializada**: duplicaría el modelo de Authoring dentro de otro servicio.
- **BFF dedicado**: componente mínimo y aislado, con su propia política de degradación.

## Decisión
Un **BFF de composición** expone la vista compuesta, con timeout y Circuit Breaker por dependencia.

Las dos fuentes se consultan **en secuencia, no en paralelo**: el catálogo se consulta por
`CourseId` y esos identificadores solo los conoce Learning, de modo que ninguna llamada a Authoring
puede construirse antes de que Learning responda. Resuelto Learning, los enriquecimientos hacia
Course Authoring son independientes entre sí y se ejecutan **de forma concurrente y acotada**. El
BFF compone después ambas fuentes.

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
- La vista sobrevive a la caída del enriquecimiento, con un contrato de degradación explícito.
- Gateway libre de lógica de composición.

## Consecuencias negativas
- Un componente adicional que desplegar y observar.
- Los clientes deben interpretar `isPartial`.

## Riesgos residuales
Latencia acumulada si ambas dependencias responden lento; acotada por timeouts.

## Decisiones relacionadas
[ADR-0007](./ADR-0007-base-de-datos-por-servicio.md) · [ADR-0017](./ADR-0017-api-gateway-con-yarp.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md)
