# ADR-0002 — Edición de cursos publicados y Finalización como hecho histórico

- **Estado:** Aceptado
- **Fecha:** 2026-07-13
- **Pendiente:** la naturaleza del Certificado se decide por separado.

## 1. Contexto

Un Instructor puede modificar un Curso que ya tiene estudiantes matriculados. El **Progreso**
del estudiante se apoya en las **Lecciones** que el Instructor puede cambiar. Es el punto de
acoplamiento más fuerte del dominio y afecta a la invariante "Curso completado".

## 2. Problema

¿Qué ocurre con el progreso cuando el contenido de un curso publicado cambia? ¿Cuándo y cómo se
considera "completada" una Matrícula?

## 3. Alternativas consideradas

**Edición de curso:**

- **Copia de trabajo con republicación explícita**: el Instructor edita en privado y decide cuándo
  el cambio llega a los estudiantes.
- **Versionado inmutable** del contenido publicado: exacto, pero obliga a modelar y mantener
  versiones que nadie ha pedido todavía.
- **Snapshot por matrícula**: congela el contenido para cada estudiante; multiplica el
  almacenamiento y deja el catálogo desincronizado con lo que cada uno ve.
- **Bloquear la edición cuando hay matriculados**: descartada, hace inmantenible un curso vivo.

**Finalización:**

- **Automática al cumplir el criterio**, sin intervención del estudiante.
- **Solo por acción explícita del estudiante**: genera un estado atascado si nunca la reclama.
- **Elegibilidad revocable más finalización observada**: separa el criterio cumplido del hecho
  sellado.

## 4. Decisión

- **Edición mediante copia de trabajo:** el Instructor edita una copia; los cambios salen al
  **republicar**.
- **Publicación:** un Curso requiere **≥ 1 Lección** para pasar a `Published` (invariante de negocio).
- **Finalización = hecho histórico inmutable.** Una vez completado, permanece completado.
- **Elegibilidad = estado revocable.** Regla única: `CourseCompleted` se produce cuando el criterio
  (100 % de las lecciones actuales) se cumple **y** es observado durante una interacción del
  estudiante. Mientras se cumple pero no se observa, el estado es `EligibleForCompletion`.
- **Contenido nuevo tras completar:** no invalida el logro ni genera un certificado nuevo.

## 5. Justificación

La copia de trabajo cubre la necesidad real —editar sin sorprender al estudiante a mitad de curso—
sin el coste permanente del versionado ni del snapshot por matrícula. Modelar la Finalización como
hecho inmutable es semánticamente correcto (los hechos no se des-ocurren) y **reduce el
acoplamiento** entre Autoría y Aprendizaje. La regla única elimina la asimetría entre el camino
normal y el caso borde.

## 6. Consecuencias

- Aparecen **dos conceptos distintos**: `Progress` (vivo, mutable, informativo) y `Completion`
  (histórico, inmutable, autoridad del logro). No deben confundirse en consultas ni proyecciones.
- La señal de elegibilidad es **no monótona** (revocable): los consumidores de eventos deben poder
  compensar.
- Son posibles las finalizaciones colgadas (elegible que nunca se observa): aceptable en un MVP
  gratuito.
- Empuja el **Certificado hacia un snapshot inmutable**, que se decidirá aparte.
- El paso futuro a versionado inmutable queda como costura de evolución, sin rediseño.
- El **modelo táctico** (agregados y máquina de estados de la Matrícula) se diseña dentro de su
  Bounded Context, no en esta decisión.
