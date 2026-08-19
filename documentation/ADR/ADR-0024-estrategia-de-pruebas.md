# ADR-0024 — Estrategia de pruebas

## Estado
Aceptado — 2026-07-16

## Contexto
Los comportamientos más caros de este sistema —idempotencia, resiliencia, compensación de la Saga y
autorización— solo se manifiestan cuando varios servicios interactúan, y no se pueden verificar
leyendo el código de uno solo.

## Problema
¿Qué niveles de prueba se adoptan y qué cubre cada uno?

## Alternativas consideradas
- **Solo pruebas manuales con una colección**: no repetible y no protege frente a regresiones.
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

**Colección de peticiones manual** con 18 escenarios, organizados en carpetas: camino feliz,
idempotencia, resiliencia, Saga y seguridad.

## Justificación
Cada comportamiento se prueba en el nivel donde realmente ocurre: las invariantes en el dominio, la
deduplicación contra una base real y la compensación contra el broker.

## Consecuencias positivas
- Verificación repetible en cada cambio.
- Los errores de idempotencia y compensación se detectan pronto.

## Consecuencias negativas
- Esfuerzo de preparación de entornos de prueba con contenedores.

## Riesgos residuales
Las pruebas de escalado y recuperación dependen del entorno de Kubernetes disponible.

## Decisiones relacionadas
[ADR-0020](./ADR-0020-docker-y-docker-compose.md) · [ADR-0021](./ADR-0021-despliegue-en-kubernetes.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md)
