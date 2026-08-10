# ADR-T10 — CQRS en Learning

## Estado
Aceptado — 2026-07-16

## Contexto
Las rúbricas exigen CQRS real en al menos un microservicio. Learning es el Core Domain: sus
escrituras protegen invariantes ricas y sus consultas necesitan vistas (estado, porcentaje, listas
por estado) que no deben calcularse sobre el agregado.

## Problema
¿Dónde y cómo se implementa CQRS sin convertirlo en un gesto decorativo?

## Alternativas consideradas
- **CQRS en todos los servicios**: coste sin beneficio en contextos sin consultas ricas.
- **CQRS nominal** (renombrar métodos): no satisface el criterio.
- **CQRS en Learning**: es donde existen consultas con forma distinta a la del agregado.
- **Event Sourcing**: descartado; no hay evidencia que lo exija y su coste es desproporcionado.

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
- Evidencia académica clara: dos modelos, dos rutas, un diagrama.

## Consecuencias negativas
- Proyección que mantener y consistencia eventual que explicar en la interfaz.

## Riesgos residuales
Desfase temporal entre escritura y lectura; se comunica marcando el porcentaje como aproximado.

## Relación con criterios académicos
Curso 2: CQRS con separación real de comandos, consultas y modelos.

## Decisiones relacionadas
[T03](./ADR-T03-clean-architecture.md) · [T12](./ADR-T12-current-lesson-set.md) · [T21](./ADR-T21-testing-strategy.md)
