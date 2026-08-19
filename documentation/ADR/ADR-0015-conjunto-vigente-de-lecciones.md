# ADR-0015 — Conjunto actual de LessonIds en Learning

## Estado
Aceptado — 2026-07-16

## Contexto
Learning necesita el conjunto actual de `LessonIds` de un curso para validar que una lección
pertenece al curso, para calcular el 100 % y para congelar el snapshot al sellar la Finalización.
Ese conjunto pertenece a Course Authoring y **no forma parte del estado del agregado**.

Un comando de escritura **puede sellar la Finalización en la misma invocación** (marcar la última
lección finaliza el curso), de modo que **cualquier escritura puede ser el paso irreversible**.

## Problema
¿Cómo obtiene Learning ese conjunto sin sellar nunca contra información obsoleta?

## Alternativas consideradas
- **Consulta síncrona fresca en cada escritura**: exactitud máxima; la disponibilidad de Learning
  queda condicionada a Authoring.
- **Proyección local actualizada por eventos**: mejor disponibilidad, pero introduce eventos, Inbox,
  reconciliación, reconstrucción y una ventana de obsolescencia difícil de acotar.
- **Versión o token de contenido publicado**: detecta obsolescencia pero añade complejidad sin
  resolver la disponibilidad.
- **Caché para marcar y validación previa al sellado**: ambigua, porque marcar puede sellar en la
  misma operación; obliga a ramificar el flujo.

## Decisión
**Consulta síncrona fresca a Course Authoring en toda escritura de Learning**
(`MarcarLeccionComoCompletada` y `ConfirmarFinalizacion`).

Si Authoring no está disponible: timeout acotado, Circuit Breaker, **respuesta 503**, **no se modifica
el agregado y no se sella la Finalización**.

La **caché queda limitada al lado de lectura** y debe identificarse como **potencialmente aproximada**.
`CursoPublicado` y `ContenidoPublicadoModificado` **no se transforman en Integration Events**.

## Justificación
Prioridad: no finalizar con datos obsoletos. Como cualquier escritura puede sellar, la única
estrategia sin ambigüedad es obtener siempre el conjunto fresco. Además evita infraestructura
innecesaria y no obliga a publicar eventos sin consumidor.

## Consecuencias positivas
- Nunca se sella contra información obsoleta.
- Camino de escritura sin caché y sin ramas: simple de razonar y de probar.
- Comportamiento ante fallos explícito: Circuit Breaker y fail-safe en un único punto.

## Consecuencias negativas
- Marcar lecciones **no está disponible** durante una caída de Authoring.
- Latencia adicional por llamada externa en cada escritura.

## Riesgos residuales
Carrera entre la comprobación y el sellado, acotada a la misma operación y documentada como riesgo
aceptado. Si la disponibilidad durante caídas de Authoring llegara a ser un requisito, se reevaluaría
la proyección local.

## Decisiones relacionadas
[ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0013](./ADR-0013-cqrs-en-learning.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md)
