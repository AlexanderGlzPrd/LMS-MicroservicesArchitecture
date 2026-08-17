# ADR-T21 — Estrategia de pruebas

## Estado
Aceptado — 2026-07-16

## Contexto
Las rúbricas exigen demostrar el flujo completo y evidenciar idempotencia, resiliencia, compensación
de la Saga y seguridad. Las pruebas manuales no bastan como única evidencia.

## Problema
¿Qué niveles de prueba se adoptan y qué evidencia produce cada uno?

## Alternativas consideradas
- **Solo pruebas manuales con una colección**: insuficiente y no repetible.
- **Solo pruebas unitarias**: no demuestra el comportamiento distribuido.
- **Pirámide adaptada al carácter distribuido de la solución**.

## Decisión

| Nivel | Alcance |
|---|---|
| Unitarias de dominio | invariantes: mínimo de una lección para publicar, sellado único, inmutabilidad |
| De aplicación | handlers con puertos simulados; comportamiento fail-safe |
| Integración | agregado y Outbox en una misma transacción local; restricciones de unicidad |
| Contratos | expectativas mínimas del consumidor sobre cada mensaje |
| Componente | servicio con su base y el broker en contenedores |
| Extremo a extremo | flujo completo a través del Gateway |
| Resiliencia | duplicados, cola de mensajes muertos, Circuit Breaker, servicio caído |
| Saga | camino de éxito **y camino compensado** |
| Seguridad | sin token, rol incorrecto e identificador ajeno en el cuerpo (debe ignorarse) |

**Colección de pruebas manual** con 18 escenarios, organizados en carpetas: camino feliz,
idempotencia, resiliencia, Saga y seguridad.

## Justificación
Cada criterio académico queda asociado a una prueba concreta que lo evidencia, y los comportamientos
distribuidos se prueban donde realmente ocurren.

## Consecuencias positivas
- Evidencia repetible y verificable.
- Los errores de idempotencia y compensación se detectan pronto.

## Consecuencias negativas
- Esfuerzo de preparación de entornos de prueba con contenedores.

## Riesgos residuales
Las pruebas de escalado y recuperación dependen del entorno de Kubernetes disponible.

## Relación con criterios académicos
Curso 1: flujo completo demostrable. Curso 2: idempotencia, resiliencia, Saga. Curso 3: escalabilidad
y recuperación ante fallos.

## Decisiones relacionadas
[T17](./ADR-T17-docker-compose.md) · [T18](./ADR-T18-kubernetes.md) · [T19](./ADR-T19-resiliencia.md) · [T20](./ADR-T20-versionado-de-contratos.md)
