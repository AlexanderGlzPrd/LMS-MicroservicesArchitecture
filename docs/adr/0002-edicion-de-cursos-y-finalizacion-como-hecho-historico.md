# ADR-0002: Edición de cursos publicados y Finalización como hecho histórico

- **Estado:** Aceptado
- **Fecha:** 2026-07-13
- **Relacionado:** Decisión 2 (naturaleza del Certificado) queda **pendiente**.

## 1. Contexto

Un Instructor puede modificar un Curso que ya tiene estudiantes matriculados. El **Progreso**
del estudiante se apoya en las **Lecciones** que el Instructor puede cambiar. Esto es el punto
de acoplamiento más fuerte del dominio y afecta a la invariante "Curso completado".

## 2. Problema

(a) ¿Qué ocurre con el progreso cuando el contenido de un curso publicado cambia?
(b) ¿Cuándo y cómo una Matrícula se considera "completada"?

## 3. Alternativas consideradas

**Edición de curso:** (A/E) mutación en vivo con copia de trabajo + republicación explícita ·
(B) versionado inmutable · (C) snapshot por matrícula · (D) bloqueo con matriculados.

**Finalización:** (3a) automática al cumplir el criterio · (3b) solo por acción del estudiante
(genera estado atascado) · (3c) reencuadrada: elegibilidad revocable + finalización observada.

## 4. Decisión

- **Edición: A/E** — el Instructor edita una copia de trabajo; los cambios salen al **republicar**.
- **Publicación:** un Curso requiere **≥ 1 Lección** para pasar a `Published` (invariante de negocio).
- **Finalización = hecho histórico inmutable.** Una vez completado, permanece completado.
- **Elegibilidad = estado revocable.** Regla única: *`CourseCompleted` se produce cuando el
  criterio (100% de las lecciones actuales) se cumple **y** es observado durante una interacción
  del estudiante.* Mientras se cumple pero no se observa → `EligibleForCompletion`.
- **Contenido nuevo tras completar = "bonus" (2a):** no invalida el logro ni genera nuevo certificado.

## 5. Justificación

A/E da ~90% del valor con ~20% del coste y evita sobre-ingeniería (vs. versionado/snapshot).
Modelar la Finalización como evento de dominio **inmutable** es semánticamente correcto
("los hechos no se des-ocurren") y **reduce el acoplamiento** entre Autoría y Aprendizaje.
La regla única elimina la asimetría entre el camino normal y el caso borde.

## 6. Consecuencias

- Aparecen **dos conceptos distintos**: `Progress` (vivo, mutable, informativo) y `Completion`
  (histórico, inmutable, autoridad del logro). No deben confundirse en consultas ni proyecciones.
- La señal de elegibilidad es **no monótona** (revocable): los consumidores de eventos deben
  poder compensar.
- Posibles **finalizaciones "colgadas"** (elegible que nunca reclama): aceptable en un MVP gratuito.
- Empuja el **Certificado hacia un snapshot inmutable** (se resolverá en la Decisión 2).
- Se deja para las **costuras de evolución** el paso futuro a versionado (B) sin rediseño.
- El **modelo táctico** (aggregates y máquina de estados de la Matrícula) se diseñará dentro de
  su Bounded Context, no ahora (separación estratégico/táctico, Regla 16).
