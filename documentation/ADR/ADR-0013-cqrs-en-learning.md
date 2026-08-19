# ADR-0013 — CQRS en Learning

## Estado
Aceptado — 2026-07-16

## Contexto
Learning es el Core Domain: sus escrituras protegen invariantes ricas y sus consultas necesitan
vistas (estado, porcentaje, listas por estado) con una forma que no es la del agregado y que no
debería calcularse sobre él.

## Problema
¿Dónde y cómo se separan los modelos de lectura y escritura sin convertirlo en un gesto decorativo?

## Alternativas consideradas
- **CQRS en todos los servicios**: coste sin beneficio en contextos sin consultas ricas.
- **CQRS nominal** (renombrar métodos): no separa nada; solo cambia los nombres.
- **CQRS en Learning**: es donde existen consultas con forma distinta a la del agregado.
- **Event Sourcing**: descartado; ninguna capacidad exige reconstruir el historial y su coste es
  desproporcionado.

## Decisión
CQRS **solo en `learning`**:

- **Modelo de escritura:** agregado `ProgresoDelCurso` (comandos `MarcarLeccionComoCompletada` y
  `ConfirmarFinalizacion`).
- **Modelo de lectura:** proyección con estado, número de lecciones completadas y **porcentaje**,
  actualizada por **eventos de dominio internos** (`LecciónCompletada`, `CursoFinalizado`).
- **Almacenamiento:** misma base, tablas separadas. **Consistencia eventual** entre ambos modelos.
- El **porcentaje es una vista** y debe identificarse como **potencialmente aproximado**; nunca es
  estado del agregado ni puede desencadenar la Finalización.

Aclaraciones: CQRS **no** implica Event Sourcing, **no** exige dos motores de base de datos, **no**
exige un framework y **no** consiste en renombrar métodos.

## Justificación
Separar los modelos permite consultas cómodas sin contaminar el agregado ni relajar sus invariantes.

## Consecuencias positivas
- Consultas eficientes sin tocar el modelo de escritura.
- Dos modelos y dos rutas claramente separados, fáciles de razonar por separado.

## Consecuencias negativas
- Proyección que mantener y consistencia eventual que explicar en la interfaz.

## Riesgos residuales
Desfase temporal entre escritura y lectura; se comunica marcando el porcentaje como aproximado.

## Decisiones relacionadas
[ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) · [ADR-0015](./ADR-0015-conjunto-vigente-de-lecciones.md) · [ADR-0024](./ADR-0024-estrategia-de-pruebas.md)
