# ADR-0005 — Monorepo

## Estado
Aceptado — 2026-07-16

## Contexto
La solución debe poder ejecutarse, documentarse y revisarse como una unidad. Los servicios son
independientes en despliegue, pero los desarrolla una sola persona y sus contratos cambian a la vez.

## Problema
¿Un repositorio por microservicio o un único repositorio para toda la solución?

## Alternativas consideradas
- **Repositorio por microservicio**: máxima autonomía real, historial independiente; pero dificulta
  la ejecución local, la documentación unificada y cualquier cambio que cruce servicios.
- **Monorepo**: ejecución en un comando, documentación e historial unificados, refactor coordinado de
  contratos; a costa de una autonomía de versionado menos estricta.

## Decisión
Monorepo con una única solución .NET, con carpetas separadas por servicio, despliegue y documentación.

## Justificación
Con un solo equipo, la **reproducibilidad y el cambio coordinado de contratos** pesan más que la
autonomía absoluta de repositorios. Documentación, diagramas, manifiestos y colección de pruebas
quedan en un único lugar, junto al código que describen.

## Consecuencias positivas
- Ejecución local sencilla y documentación centralizada.
- Historial de commits coherente y trazable.
- Cambios de contrato coordinados con sus pruebas.

## Consecuencias negativas
- La autonomía de despliegue debe mantenerse por disciplina, no por separación física del repositorio.
- Riesgo de acoplamiento accidental entre proyectos si no se respetan las reglas de dependencia.

## Riesgos residuales
Tentación de referenciar directamente código ajeno; se mitiga con las reglas de
[ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) y [ADR-0025](./ADR-0025-building-blocks-tecnicos.md).

## Decisiones relacionadas
[ADR-0004](./ADR-0004-servicios-desplegables-por-contexto.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) · [ADR-0025](./ADR-0025-building-blocks-tecnicos.md)
