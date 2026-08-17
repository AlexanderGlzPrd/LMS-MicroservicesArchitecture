# ADR-T02 — Monorepo

## Estado
Aceptado — 2026-07-16

## Contexto
El proyecto se evalúa en tres cursos y debe poder ejecutarse, documentarse y revisarse como una
unidad. Los servicios son independientes en despliegue pero desarrollados por una sola persona.

## Problema
¿Un repositorio por microservicio o un único repositorio para toda la solución?

## Alternativas consideradas
- **Repositorio por microservicio**: máxima autonomía real, historial independiente; pero dificulta
  la ejecución local, la documentación unificada y la evaluación.
- **Monorepo**: ejecución en un comando, documentación e historial unificados, refactor coordinado de
  contratos; a costa de una autonomía de versionado menos estricta.

## Decisión
Monorepo con una única solución .NET, con carpetas separadas por servicio, despliegue y documentación.

## Justificación
En un contexto académico la **evaluabilidad y la reproducibilidad** pesan más que la autonomía
absoluta de repositorios. Toda la evidencia (documentación, diagramas, manifiestos, colección de
pruebas) queda en un solo lugar verificable.

## Consecuencias positivas
- Ejecución local sencilla y documentación centralizada.
- Historial de commits coherente y trazable por incrementos.
- Cambios de contrato coordinados con sus pruebas.

## Consecuencias negativas
- La autonomía de despliegue debe mantenerse por disciplina, no por separación física del repositorio.
- Riesgo de acoplamiento accidental entre proyectos si no se respetan las reglas de dependencia.

## Riesgos residuales
Tentación de referenciar directamente código ajeno; se mitiga con las reglas de
[T20](./ADR-T20-versionado-de-contratos.md) y [T22](./ADR-T22-bloques-tecnicos.md).

## Relación con criterios académicos
Curso 1, 2 y 3: estructura del repositorio, documentación, historial de commits.

## Decisiones relacionadas
[T01](./ADR-T01-microservicios-fisicos.md) · [T20](./ADR-T20-versionado-de-contratos.md) · [T22](./ADR-T22-bloques-tecnicos.md)
