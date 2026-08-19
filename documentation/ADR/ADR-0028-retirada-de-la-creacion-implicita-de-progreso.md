# ADR-0028 — Retirada de la creación provisional del Progreso y excepción única a ADR-0027 §6

## Estado
Aceptado — 2026-08-15

## Contexto
`contextos-delimitados.md` §4 y `flujos-de-aplicacion.md` §3 fijan que el `ProgresoDelCurso` **nace al
reconocer el acceso concedido**, y [ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) asigna ese hecho al Integration
Event `EstudianteMatriculado` (`StudentEnrolled`), de `enrollment` a `learning`.

Cuando se implementó `learning`, ese evento todavía no existía: no había broker,
[ADR-0011](./ADR-0011-outbox-transaccional.md) no estaba implementado y `learning` no tenía **ninguna**
forma legítima de conocer una Matrícula. Para que el servicio fuese ejecutable, se admitió un camino
provisional dentro de `MarcarLeccionComoCompletada`: si el Progreso no existía, el caso de uso lo
creaba con `CourseProgress.Start(...)` y respondía `200`. Quedó declarado **provisional desde su
origen**, por escrito, y con su retirada condicionada a la llegada de `StudentEnrolled`.

Esa condición ya se cumple: existen el productor con Outbox, el broker y el consumidor idempotente
con Inbox. La causa autoritativa existe:

```
Enrollment → StudentEnrolled → Learning → CourseProgress.Start(...)
```

Mantener además la creación implícita dejaría **dos** causas permanentes de nacimiento del mismo
Aggregate Root, una de las cuales permite iniciar Progreso **sin acceso concedido**.

## Problema
Retirar la creación implícita cambia la respuesta de una ruta ya publicada en `v1`:
`POST /api/v1/me/course-progress/{courseId}/completed-lessons` pasa de `200` —creando el Progreso— a
`404 CourseProgressNotFound`. [ADR-0027](./ADR-0027-versionado-de-apis-rest.md) §6 prohíbe exactamente eso
dentro de una versión. ¿Se crea `v2` de `learning`, o se admite una excepción?

## Alternativas consideradas
- **Crear `v2` de `learning`**: formalmente impecable. Duplica controladores, contratos, documento
  OpenAPI y pruebas de forma permanente, para retirar una puerta que nació marcada como temporal y
  que ningún consumidor desplegado usa.
- **Mantener las dos causas de creación**: evita tocar el contrato y deja el agregado con dos
  orígenes, uno de ellos sin Matrícula. Contradice `contextos-delimitados.md` §4.
- **Degradar el camino provisional a "solo si no hay evento en tránsito"**: `learning` no puede
  distinguir esa situación de "nunca hubo Matrícula"; exigiría consultar a `enrollment`, que
  `matriz-de-comunicacion.md` §2 no contempla.
- **Excepción única y nominal a ADR-0027 §6**, registrada como decisión propia.

## Decisión
`MarcarLeccionComoCompletada` **deja de crear** el `ProgresoDelCurso`. Pasa a ser una operación
**sobre un Progreso que ya existe**, y cuando no existe responde `404 CourseProgressNotFound`, el
mismo código y el mismo significado que `learning` ya publica en `ConfirmarFinalizacion` y en la
consulta del Progreso por curso.

Se autoriza para ello una **excepción única, nominal y acotada** a [ADR-0027](./ADR-0027-versionado-de-apis-rest.md) §6:

> En `learning`, la ruta `POST /api/v1/me/course-progress/{courseId}/completed-lessons` deja de crear
> el `ProgresoDelCurso` cuando no existe y pasa a responder `404`, **dentro de `/api/v1`** y **sin**
> crear `v2`.

**Alcance estricto:**

1. Se aplica **solo** a esa ruta y a ese cambio. Ningún otro endpoint, campo ni código de estado de
   `learning` cambia.
2. **Se mantiene `/api/v1`.** No se crea `v2`, no se marca `v1` como obsoleta y no se publica ruta de
   compatibilidad.
3. **No modifica ningún criterio de ADR-0027**, que sigue vigente e íntegro.
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
  ([ADR-0017](./ADR-0017-api-gateway-con-yarp.md)), ni BFF ([ADR-0014](./ADR-0014-composicion-de-api-en-bff.md)), ni cliente
  externo. El coste real de compatibilidad es cero.
- El `404` **unifica** un significado en lugar de introducir uno nuevo: es el que la propia `v1` ya
  usa para "no existe Progreso del actor para ese curso" en las otras rutas que lo necesitan.

Los cuatro son **acumulativos**: la excepción nace de su combinación. La ausencia de consumidores,
por sí sola, no autoriza nada; si el comportamiento no se hubiera declarado provisional de entrada, o
si el `404` no estuviera ya publicado en `v1` con ese mismo significado, se habría creado `v2`.

Cualquier cambio incompatible futuro sigue sometido íntegramente a
[ADR-0027](./ADR-0027-versionado-de-apis-rest.md), y necesita su propia decisión registrada sin
apoyarse en esta.

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

## Decisiones relacionadas
[ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0011](./ADR-0011-outbox-transaccional.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md) · [ADR-0015](./ADR-0015-conjunto-vigente-de-lecciones.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) · [ADR-0027](./ADR-0027-versionado-de-apis-rest.md)
