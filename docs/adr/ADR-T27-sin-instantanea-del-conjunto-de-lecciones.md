# ADR-T27 — `ProgresoDelCurso` no persiste snapshot de `LessonIds`

## Estado
Aceptado — 2026-08-16

## Contexto
[T12](./ADR-T12-conjunto-de-lecciones-vigente.md) decidió que Learning obtiene el **conjunto actual de
`LessonIds` de forma síncrona y fresca en toda escritura**, y en su sección de Contexto enumeró tres
usos de ese conjunto:

> «Learning necesita el conjunto actual de `LessonIds` de un curso para validar que una lección
> pertenece al curso, para calcular el 100 % y **para congelar el snapshot al sellar la
> Finalización**.»

`docs/diagramas/secuencia-aprendizaje-certificacion.md` representaba lo mismo, con el paso
`SELLA Finalización (inmutable) + snapshot de LessonIds`.

Ese tercer uso **no se corresponde con el modelo aprobado ni con el implementado**:

- `contextos-delimitados.md` §4 enumera el estado del agregado —`StudentId`, `CourseId`, `LessonIds`
  completadas, `EnProgreso | Finalizado`, Finalización opcional e inmutable— y **no incluye ningún
  snapshot** del conjunto publicado. El mismo apartado dice, además, que **el conjunto actual de
  `LessonIds` pertenece a Authoring y no forma parte del estado del agregado**.
- Learning se implementó **sin** ese snapshot: `CourseProgress`
  sella `Status` y `CompletedAt`, y no persiste el conjunto observado.
- `matriz-de-comunicacion.md` §3 fija la información mínima de `CursoFinalizado` —`StudentId`,
  `CourseId`, fecha— y `contextos-delimitados.md` §5 excluye el detalle de lecciones del Certificado.

La incorporación de CQRS a Learning obliga a resolver la discrepancia, porque el modelo de lectura
necesita un número total de lecciones y hay que decir con precisión de dónde sale y qué es.

## Problema
¿Cuál es el estado vigente: el `snapshot` que describe el Contexto de T12 y dibujaba el diagrama, o
el agregado sin snapshot que describen `contextos-delimitados.md` §4 y el código aprobado? Y si es el
segundo, ¿qué ocurre con la afirmación de T12?

## Alternativas consideradas
- **Implementar el snapshot para hacer cierto el texto de T12**: ampliaría el modelo de escritura con
  estado nuevo y su migración, por un motivo que ninguna capacidad existente ni prevista solicita.
  Ningún consumidor lo pide: ni el Integration Event, ni el Certificado, ni el modelo de lectura.
- **Reescribir el Contexto de T12**: un ADR aceptado no se edita para acomodar un caso posterior;
  hacerlo borra el rastro de que la discrepancia existió.
- **Dejar la discrepancia viva y anotarla cada vez que alguien la tropiece**: la convierte en folclore y
  garantiza que alguien la implemente años después.
- **Un ADR que declare el estado vigente y sustituya únicamente esa afirmación**, dejando T12 intacto
  en todo lo demás.

## Decisión
**`ProgresoDelCurso` (`CourseProgress`) no persiste ningún snapshot del conjunto de `LessonIds`.**

Esta decisión **sustituye únicamente** la afirmación del Contexto de
[T12](./ADR-T12-conjunto-de-lecciones-vigente.md) según la cual el conjunto se obtiene también «para congelar el
snapshot al sellar la Finalización». **Todo el resto de T12 permanece vigente e íntegro.**

**Alcance exacto:**

1. **Learning sigue obteniendo el conjunto fresco de `LessonIds` en toda escritura**, exactamente
   como T12 exige, en `MarcarLeccionComoCompletada` y en `ConfirmarFinalizacion`. Los dos usos que
   T12 enumera y que sí siguen vigentes —validar la pertenencia de una `LessonId` y determinar el
   100 %— no cambian en absoluto.
2. **`CourseProgress` no persiste ese conjunto** y **no lo congela al sellar**. Al producirse la
   transición se sellan `Status = Completed` y `CompletedAt`, y nada más.
3. **`CursoFinalizado` (`CourseCompleted`) no transporta `LessonIds`**, ni su recuento, ni ningún
   derivado del conjunto publicado. Su contenido sigue siendo el mínimo de
   `matriz-de-comunicacion.md` §3.
4. **Certification no almacena `LessonIds`** ni detalle de lecciones, conforme a
   `contextos-delimitados.md` §5.
5. **El total de lecciones del modelo de lectura es una observación derivada, no estado del
   agregado.** Se registra en la proyección con el tamaño del conjunto observado en la escritura más
   reciente, vive **solo** en el modelo de lectura y es —en los términos de
   [T10](./ADR-T10-cqrs-en-learning.md)— **potencialmente aproximado**. Puede quedar desactualizado tras
   una republicación hasta la siguiente escritura del progreso.
6. **Las lecciones completadas de la proyección no son un snapshot del conjunto publicado.** Son
   exactamente lo que el agregado ya registra: las lecciones que el estudiante completó. Confundir
   ambas cosas es precisamente lo que esta decisión evita.
7. **El estado vigente lo definen `contextos-delimitados.md` §4 y el modelo implementado.** Cualquier lectura futura del Contexto de T12 debe interpretarse a la
   luz de este ADR.
8. **`docs/diagramas/secuencia-aprendizaje-certificacion.md` queda alineado**: el paso de sellado ya no
   representa ningún snapshot de `LessonIds`.
9. **Sin efecto en tiempo de ejecución.** Esta decisión no cambia ningún comportamiento, no exige
   ninguna migración y no retira ninguna capacidad: describe lo que el sistema ya hace y retira una
   afirmación que nunca se implementó.

## Justificación
Entre dos documentos que se contradicen, gana el que tiene autoridad sobre la materia en disputa.
`contextos-delimitados.md` §4 es el documento que **enumera el estado del agregado**, y no incluye el
snapshot; el Contexto de T12 es una motivación introductoria de una decisión cuyo objeto real es
**cómo se obtiene el conjunto**, no qué se persiste. La decisión de T12 —consulta síncrona fresca en
toda escritura— no depende en absoluto de que exista o no un snapshot, y por eso sobrevive intacta.

El mismo §4 lo dice sin ambigüedad en sentido contrario: *«el conjunto actual de `LessonIds`
pertenece a Authoring y no forma parte del estado del agregado»*. Persistirlo al sellar sería
introducir en Learning una copia de un dato ajeno, con la obsolescencia y la reconciliación que T12
descartó expresamente al rechazar la proyección local.

Y no hay ningún consumidor. El Integration Event lleva el mínimo fijado; el Certificado excluye el
detalle de lecciones; el modelo de lectura necesita un número, no un conjunto, y lo obtiene como
observación de la escritura que ya está obligada a consultarlo. Implementar el snapshot añadiría
estado permanente sin un solo lector.

Registrar esto como ADR —y no como nota dentro de una spec— es lo que impide que la discrepancia
reaparezca: quien lea solo `docs/adr/` encuentra la corrección junto a la decisión corregida.

## Consecuencias positivas
- Desaparece una contradicción entre un ADR, un diagrama, un documento de arquitectura y el código.
- El modelo de escritura queda con exactamente el estado que `contextos-delimitados.md` §4 enumera.
- La frontera con Authoring se mantiene: Learning consulta su conjunto, no lo copia.
- Queda escrito que el total del modelo de lectura es observación derivada, con lo que su carácter
  aproximado deja de ser una sorpresa.

## Consecuencias negativas
- El Contexto de T12 conserva una frase que ya no describe el sistema, y solo este ADR lo aclara.
  Es el precio de no reescribir un ADR aceptado, y es el mismo criterio que
  [T25](./ADR-T25-retirada-del-arranque-provisional-de-progreso.md) aplicó con
  [T24](./ADR-T24-versionado-de-api-rest.md).
- Si algún día hiciera falta reconstruir qué lecciones existían en el instante exacto del sellado, el
  dato no está. No hay ningún requisito que lo pida, y ADR-0002 §4 ya declara que el contenido
  posterior no invalida el logro.

## Riesgos residuales
Que alguien lea el Contexto de T12 aisladamente y vuelva a implementar el snapshot. Se mitiga con
este ADR, enlazado desde el índice, y con el alineamiento del diagrama, que era la otra fuente que lo
representaba.

## Relación con criterios académicos
Curso 1: Domain Modeling y fronteras de contexto. Curso 2: CQRS, consistencia eventual y contratos de
Integration Events.

## Decisiones relacionadas
[0002](./0002-edicion-de-cursos-y-finalizacion-como-hecho-historico.md) · [T06](./ADR-T06-comunicacion.md) · [T10](./ADR-T10-cqrs-en-learning.md) · [T12](./ADR-T12-conjunto-de-lecciones-vigente.md) · [T20](./ADR-T20-versionado-de-contratos.md) · [T25](./ADR-T25-retirada-del-arranque-provisional-de-progreso.md)
