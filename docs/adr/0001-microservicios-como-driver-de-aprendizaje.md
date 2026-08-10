# ADR-0001: Microservicios como driver de aprendizaje

- **Estado:** Aceptado
- **Fecha:** 2026-07-12

## 1. Contexto

Proyecto formativo: construir un LMS con un solo desarrollador. El objetivo primario
**no** es entregar producto rápido, sino **aprender arquitectura de microservicios y
sistemas distribuidos**. El sistema evolucionará en tres etapas (microservicios/DDD →
EDA/CQRS/SAGA → Cloud-Native/Kubernetes/Keycloak).

## 2. Problema

Para un MVP de este alcance y con un solo equipo, ¿qué estilo arquitectónico adoptar:
monolito modular o microservicios?

## 3. Alternativas consideradas

- **Monolito modular.** Un despliegue, módulos bien separados internamente.
  - ✅ Simplicidad operativa, consistencia transaccional, refactor barato.
  - ❌ No ejercita el "impuesto del sistema distribuido" que se quiere aprender.
- **Microservicios.**
  - ✅ Despliegue/escalado independientes; ejercita comunicación, resiliencia, EDA, SAGA.
  - ❌ Complejidad operativa y de consistencia desde el día 1 (sobre-ingeniería para el negocio).
- **Monolito sin modularizar ("big ball of mud").** Descartado de entrada.

## 4. Decisión

Adoptar **microservicios**, con una **regla de oro**: cada frontera de servicio debe
**justificarse desde el dominio** (un Bounded Context real), nunca por capricho técnico.

## 5. Justificación

El aprendizaje de sistemas distribuidos es, en este proyecto, un **requisito de primera
clase**, tan válido como uno de negocio. Para un producto real, un monolito modular sería
la elección correcta; aquí, el objetivo educativo cambia la ecuación. La regla de oro
mantiene la honestidad de diseño (alineada con la Regla 4: cada patrón debe justificarse
en el dominio).

## 6. Consecuencias

- Asumimos **conscientemente** el coste del sistema distribuido (red, consistencia eventual, operación).
- Ningún servicio se crea sin un Bounded Context que lo justifique.
- Se enseñan igualmente los principios de monolito modular: un buen microservicio es,
  internamente, un módulo bien diseñado.
- Riesgo asumido: mayor complejidad de depuración y despliegue, aceptable por el fin formativo.
