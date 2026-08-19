# ADR-0020 — Docker y Docker Compose

## Estado
Aceptado — 2026-07-16

## Contexto
La solución debe poder ejecutarse completa en local para probar el flujo íntegro con una colección
de peticiones, y el entorno local debe parecerse al de despliegue.

## Problema
¿Cómo se contenedoriza y se orquesta la solución en local?

## Alternativas consideradas
- **Ejecución sin contenedores**: obliga a instalar y configurar a mano broker, bases y proveedor de
  identidad, y el entorno deja de ser reproducible.
- **Contenedores individuales sin orquestación local**: arranque manual y frágil.
- **Docker Compose**: un solo comando levanta todo el stack con dependencias y comprobaciones de salud.

## Decisión
Un archivo de construcción de imagen por unidad desplegable y **Docker Compose** para la ejecución
local del stack completo: los seis servicios de aplicación, el Gateway, el BFF, el broker, Keycloak,
las persistencias y la observabilidad, en una red común, con variables externalizadas y
comprobaciones de salud con dependencias ordenadas.

**Orden lógico de validación:** infraestructura → Course Authoring → Enrollment → Learning →
Certification → Gateway y BFF → observabilidad → compra de acceso.

## Justificación
Es el mecanismo estándar para reproducir el entorno completo con un comando, y el que menos se aleja
del despliegue en Kubernetes.

## Consecuencias positivas
- Entorno reproducible y flujo completo ejecutable en una máquina.
- Paridad razonable con el despliegue en Kubernetes.

## Consecuencias negativas
- Consumo de recursos considerable en la máquina local.
- Tiempos de arranque perceptibles.

## Riesgos residuales
Arranque en frío de Keycloak y del broker más lento que el de los servicios; se mitiga con
comprobaciones de salud y dependencias condicionadas.

## Decisiones relacionadas
[ADR-0007](./ADR-0007-base-de-datos-por-servicio.md) · [ADR-0021](./ADR-0021-despliegue-en-kubernetes.md) · [ADR-0024](./ADR-0024-estrategia-de-pruebas.md)
