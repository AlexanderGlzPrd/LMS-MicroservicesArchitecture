# ADR-T17 — Docker y Docker Compose

## Estado
Aceptado — 2026-07-16

## Contexto
La solución debe poder ejecutarse completa en local para probar el flujo íntegro y demostrarlo con
una colección de pruebas, y todos los servicios deben estar contenerizados.

## Problema
¿Cómo se contenedoriza y se orquesta la solución en local?

## Alternativas consideradas
- **Ejecución sin contenedores**: no cumple el criterio y dificulta la reproducibilidad.
- **Contenedores individuales sin orquestación local**: arranque manual y frágil.
- **Docker Compose**: un solo comando levanta todo el stack con dependencias y comprobaciones de salud.

## Decisión
Un archivo de construcción de imagen por unidad desplegable y **Docker Compose** para la ejecución
local del stack completo: los seis servicios de aplicación, el Gateway, el BFF, el broker, Keycloak,
las persistencias y la observabilidad, en una red común, con variables externalizadas y
comprobaciones de salud con dependencias ordenadas.

**Orden lógico de validación:** infraestructura → Course Authoring → Enrollment → Learning →
Certification → Gateway y BFF → observabilidad → extensión de Saga.

## Justificación
Es el mecanismo estándar para reproducir el entorno completo con un comando, requisito directo de
la evaluación.

## Consecuencias positivas
- Reproducibilidad y demostración sencilla del flujo completo.
- Paridad razonable con el despliegue en Kubernetes.

## Consecuencias negativas
- Consumo de recursos considerable en la máquina local.
- Tiempos de arranque perceptibles.

## Riesgos residuales
Arranque en frío de Keycloak y del broker más lento que el de los servicios; se mitiga con
comprobaciones de salud y dependencias condicionadas.

## Relación con criterios académicos
Curso 1: Dockerfile por microservicio, Docker Compose, ejecución del flujo completo. Curso 3: Docker.

## Decisiones relacionadas
[T04](./ADR-T04-database-per-service.md) · [T18](./ADR-T18-kubernetes.md) · [T21](./ADR-T21-testing-strategy.md)
