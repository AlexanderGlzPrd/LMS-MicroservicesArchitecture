# ADR-0001 — Estilo arquitectónico basado en microservicios

- **Estado:** Aceptado
- **Fecha:** 2026-07-12

## 1. Contexto

El sistema es un LMS que se construirá por etapas: primero el modelo de dominio y la separación
en contextos, después la comunicación por eventos con CQRS y Saga, y finalmente el despliegue en
Kubernetes con un proveedor de identidad externo. Cada etapa añade capacidades que evolucionan a
ritmos distintos y que no comparten ni las mismas reglas ni el mismo perfil de carga.

## 2. Problema

Con ese alcance y una única línea de desarrollo, ¿qué estilo arquitectónico adoptar: monolito
modular o microservicios?

## 3. Alternativas consideradas

- **Monolito modular.** Un despliegue, módulos bien separados internamente.
  - Simplicidad operativa, consistencia transaccional, refactor barato.
  - Las fronteras entre módulos se erosionan sin una barrera física que las sostenga, y el
    despliegue y el escalado quedan atados a la unidad más lenta.
- **Microservicios.**
  - Despliegue y escalado independientes por contexto; fronteras de propiedad verificables.
  - Complejidad operativa y consistencia eventual desde el primer día.
- **Monolito sin modularizar.** Descartado de entrada.

## 4. Decisión

Adoptar **microservicios**, con una **regla de oro**: cada frontera de servicio debe
**justificarse desde el dominio** (un Bounded Context real), nunca por conveniencia técnica.

## 5. Justificación

Las capacidades del LMS tienen dueños, invariantes y ciclos de cambio distintos: la autoría del
contenido, la concesión de acceso, el progreso del estudiante y la certificación no cambian juntas
ni fallan juntas. Separarlas físicamente convierte esa diferencia en una propiedad del sistema y no
en una convención que depende de la disciplina de quien escribe el código.

La regla de oro es lo que impide que el estilo degenere: sin un Bounded Context detrás, un servicio
nuevo solo añade latencia y modos de fallo.

## 6. Consecuencias

- Se asume el coste del sistema distribuido: red, consistencia eventual y operación.
- Ningún servicio se crea sin un Bounded Context que lo justifique.
- Internamente, cada servicio se diseña como un módulo bien delimitado: la separación física no
  sustituye a la separación lógica.
- Riesgo asumido: mayor complejidad de depuración y despliegue.
