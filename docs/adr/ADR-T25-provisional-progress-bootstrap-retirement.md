# ADR-T25 — Retirada de la creación provisional del Progreso y excepción única a T24 §6

## Estado
Aceptado — 2026-08-15

## Contexto
`bounded-contexts.md` §4 y `application-flows.md` §3 fijan que el `ProgresoDelCurso` **nace al
reconocer el acceso concedido**, y [T06](./ADR-T06-communication.md) asigna ese hecho al Integration
Event `EstudianteMatriculado` (`StudentEnrolled`), de `enrollment` a `learning`.

Cuando se implementó `learning`, ese evento todavía no existía: no había broker,
[T08](./ADR-T08-transactional-outbox.md) no estaba implementado y `learning` no tenía **ninguna**
forma legítima de conocer una Matrícula. Para que el servicio fuese ejecutable, se admitió un camino
provisional dentro de `MarcarLeccionComoCompletada`: si el Progreso no existía, el caso de uso lo
creaba con `CourseProgress.Start(...)` y respondía `200`. Se declaró **provisional desde su origen**,
por escrito, y se nombró el incremento que lo retiraría: el que introdujera `StudentEnrolled`.

Ese incremento ya introduce el productor con Outbox, el broker y el consumidor idempotente con Inbox.
La causa autoritativa existe:

```
Enrollment → StudentEnrolled → Learning → CourseProgress.Start(...)
```

Mantener además la creación implícita dejaría **dos** causas permanentes de nacimiento del mismo
Aggregate Root, una de las cuales permite iniciar Progreso **sin acceso concedido**.

## Problema
Retirar la creación implícita cambia la respuesta de una ruta ya publicada en `v1`:
`POST /api/v1/me/course-progress/{courseId}/completed-lessons` pasa de `200` —creando el Progreso— a
`404 CourseProgressNotFound`. [T24](./ADR-T24-rest-api-versioning.md) §6 prohíbe exactamente eso
dentro de una versión. ¿Se crea `v2` de `learning`, o se admite una excepción?

## Alternativas consideradas
- **Crear `v2` de `learning`**: formalmente impecable. Duplica controladores, contratos, documento
  OpenAPI y pruebas de forma permanente, para retirar una puerta que nació marcada como temporal y
  que ningún consumidor desplegado usa.
- **Mantener las dos causas de creación**: evita tocar el contrato y deja el agregado con dos
  orígenes, uno de ellos sin Matrícula. Contradice `bounded-contexts.md` §4.
- **Degradar el camino provisional a "solo si no hay evento en tránsito"**: `learning` no puede
  distinguir esa situación de "nunca hubo Matrícula"; exigiría consultar a `enrollment`, que
  `communication-matrix.md` §2 no contempla.
- **Excepción única y nominal a T24 §6**, registrada como decisión propia.

## Decisión
`MarcarLeccionComoCompletada` **deja de crear** el `ProgresoDelCurso`. Pasa a ser una operación
**sobre un Progreso que ya existe**, y cuando no existe responde `404 CourseProgressNotFound`, el
mismo código y el mismo significado que `learning` ya publica en `ConfirmarFinalizacion` y en la
consulta del Progreso por curso.

Se autoriza para ello una **excepción única, nominal y acotada** a [T24](./ADR-T24-rest-api-versioning.md) §6:

> En `learning`, la ruta `POST /api/v1/me/course-progress/{courseId}/completed-lessons` deja de crear
> el `ProgresoDelCurso` cuando no existe y pasa a responder `404`, **dentro de `/api/v1`** y **sin**
> crear `v2`.

**Alcance estricto:**

1. Se aplica **solo** a esa ruta y a ese cambio. Ningún otro endpoint, campo ni código de estado de
   `learning` cambia.
2. **Se mantiene `/api/v1`.** No se crea `v2`, no se marca `v1` como obsoleta y no se publica ruta de
   compatibilidad.
3. **No modifica ningún criterio de T24**, que sigue vigente e íntegro.
4. **No debilita ni sustituye la regla general de versionado.** Cualquier otro cambio incompatible
   dentro de una versión sigue exigiendo `v2`.
5. **No es precedente genérico.** No se deriva de aquí ninguna regla del tipo "se pueden cambiar
   códigos de estado cuando no hay consumidores": cualquier caso futuro exige su propia decisión
   registrada, con sus propias razones.
6. No se extiende a `course-authoring` ni a `enrollment`, cuyos contratos `v1` no cambian.

## Justificación
Cuatro hechos, y los cuatro son verificables:

- El comportamiento retirado se declaró **provisional desde su origen**, no es un cambio sobrevenido.
- Su retirada estaba **prevista expresamente** para el momento en que existiera `StudentEnrolled`.
- **No existe ningún consumidor desplegado** del contrato de `learning`: no hay Gateway
  ([T14](./ADR-T14-yarp-gateway.md)), ni BFF ([T11](./ADR-T11-api-composition.md)), ni cliente
  externo. El coste real de compatibilidad es cero.
- El `404` **unifica** un significado en lugar de introducir uno nuevo: es el que la propia `v1` ya
  usa para "no existe Progreso del actor para ese curso" en las otras rutas que lo necesitan.

**Los cuatro hechos son acumulativos, y la excepción nace de su combinación, no de ninguno por
separado.** En particular, **la ausencia de consumidores desplegados es solo uno de los cuatro** y
**no autoriza por sí sola** ninguna excepción: si el comportamiento retirado no se hubiera declarado
provisional desde su origen, si su retirada no estuviera prevista de antemano, o si el `404` no
estuviera ya publicado en `v1` con ese mismo significado, esta decisión **no** se habría tomado.
Ninguno de los cuatro hechos, aisladamente, basta.

De ahí que la excepción quede atada a este caso concreto y a estas cuatro condiciones registradas.
**Cualquier cambio incompatible futuro sigue sometido íntegramente a
[T24](./ADR-T24-rest-api-versioning.md)**, y quien pretenda una excepción necesita **su propio ADR**,
con sus propios hechos, sin poder apoyarse en este. Registrar la decisión aquí —y no dentro de una
spec de implementación— es precisamente lo que impide que se convierta en precedente tácito.

## Consecuencias positivas
- El `ProgresoDelCurso` queda con **una sola** causa de nacimiento, y es la autoritativa.
- Desaparece la posibilidad de iniciar Progreso sin acceso concedido.
- `404 CourseProgressNotFound` mantiene un único significado en las tres rutas de `learning` que
  requieren un progreso concreto: no existe Progreso del actor para ese curso.
- La ventana de consistencia eventual queda visible y honesta: `201` en Enrollment y, brevemente,
  `404` en Learning hasta que el evento se consume.

## Consecuencias negativas
- Un cliente hipotético que dependiera del `200` anterior deja de funcionar. Se acepta: no existe.
- Aparece una excepción a una regla general, que hay que vigilar para que no se generalice.

## Riesgos residuales
Que la excepción se cite como autorización genérica en decisiones futuras. Se mitiga con el alcance
estricto declarado arriba y con la exigencia de que cualquier caso nuevo tenga su propio ADR.

## Relación con criterios académicos
Curso 2: evolución de contratos, despliegue independiente de servicios, EDA y consistencia eventual.

## Decisiones relacionadas
[T06](./ADR-T06-communication.md) · [T08](./ADR-T08-transactional-outbox.md) · [T09](./ADR-T09-inbox-deduplication.md) · [T12](./ADR-T12-current-lesson-set.md) · [T20](./ADR-T20-contract-versioning.md) · [T24](./ADR-T24-rest-api-versioning.md)
