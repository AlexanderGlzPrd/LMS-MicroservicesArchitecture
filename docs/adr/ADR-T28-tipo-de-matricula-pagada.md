# ADR-T28 — Alcance de la cláusula «no se modifica el Aggregate Root» de T23

## Contexto
`docs/lenguaje-ubicuo.md` §82 define `TipoDeMatricula` como `Free` | `Paid` y añade que *«en el MVP
siempre es `Free`»*. `subdominios.md` §28 lo repite. El código, en cambio, declara
`enum EnrollmentType { Free = 1 }`: hasta ahora no existía otra vía de concesión, así que el segundo
valor nunca llegó a escribirse.

[T13](./ADR-T13-saga-matricula-de-pago.md) introduce la Saga de compra de acceso y
[T23](./ADR-T23-comando-matricula-de-pago.md) fija cómo Enrollment participa en ella: recibe
`ConcederMatriculaPorPagoCapturado`, crea la Matrícula y registra la concesión en un ledger por
`PurchaseId`. Para acotar esa apertura, T23 declara literalmente:

> No se modifican: el Aggregate Root `Matrícula`, sus invariantes (curso publicado y unicidad por
> estudiante y curso) ni el evento producido.

Leída al pie de la letra, esa frase prohíbe también **añadir el valor que el lenguaje ubicuo ya
define**, porque `EnrollmentType` es un tipo interno del agregado.

## Problema
¿Puede la concesión pagada grabar `TipoDeMatricula = Paid` sin contradecir la cláusula de T23, o la
implementación debe grabar `Free` para una Matrícula obtenida mediante un pago capturado?

## Alternativas consideradas
- **Grabar `Free` en la concesión pagada.** Respeta la letra de T23 sin tocar nada, y a cambio
  escribe un dato falso en la única tabla que documenta cómo se obtuvo el acceso. La distinción
  entre matrícula gratuita y pagada dejaría de existir en el sistema justo cuando empieza a existir
  de verdad.
- **Añadir una columna nueva al agregado**, del estilo `AcquiredBy` o `PurchaseId`. Duplica un
  concepto que el lenguaje ubicuo ya nombra, y `fiabilidad-e-idempotencia.md` §5 prohíbe
  expresamente que el agregado incorpore `PurchaseId`.
- **Editar T23** para retirar o matizar la cláusula. T23 está **Aceptado con riesgos residuales**
  desde 2026-07-16 y su fecha documenta cuándo se tomó la decisión; reescribirlo borra esa traza.
- **Precisar el alcance de la cláusula en un ADR nuevo**, dejando T23 intacto.

## Decisión
Se precisa el alcance de la cláusula de T23 sin editarla.

> La cláusula «no se modifica el Aggregate Root `Matrícula`» de [T23](./ADR-T23-comando-matricula-de-pago.md)
> protege **las invariantes del agregado, su clave natural y el evento que produce**. No protege el
> conjunto de valores de `TipoDeMatricula`, que el lenguaje ubicuo había definido como `Free` | `Paid`
> antes de que existiera la Saga.

En consecuencia:

1. `EnrollmentType` gana el valor `Paid = 2`, el que §82 ya nombraba.
2. Se añade la factoría `Enrollment.GrantPaid(id, studentId, courseId, grantedAt)`, hermana de
   `GrantFree`.
3. **Las invariantes no cambian.** Curso publicado y unicidad por `(StudentId, CourseId)` se
   verifican exactamente igual en ambas factorías.
4. **La clave natural no cambia.**
5. **El evento no cambia.** `StudentEnrolled` conserva su contrato V1 —`StudentId`, `CourseId`,
   `OccurredAt`— y su exchange. Learning no se entera de nada y no se modifica.
6. **El agregado no incorpora `PurchaseId`.** La correlación con la compra vive en el ledger
   `purchase_grants`, que es un registro de aplicación y no lo referencia el dominio.
7. T23 **no se edita** y permanece *Aceptado con riesgos residuales* con su fecha original.

## Justificación
La cláusula de T23 existe para acotar una apertura: impedir que Enrollment relaje sus reglas o
cambie lo que publica por el hecho de aceptar un comando nuevo. Los tres bienes que protege son las
invariantes, la clave natural y el evento, y los tres quedan intactos. El conjunto de valores de un
tipo interno no pertenece a esa lista: no es una regla que se relaje ni un contrato que otros
consuman.

Grabar `Free` sería la única alternativa que respeta la letra de T23 sin añadir nada, y produce un
dato incorrecto en la tabla que documenta el origen del acceso. Un ADR existe precisamente para no
tener que elegir entre una lectura literal y un sistema que dice la verdad.

Y hay un argumento de precedencia: el lenguaje ubicuo definió `Paid` antes de que existiera esta
Saga. La implementación no está inventando un valor nuevo, está terminando de escribir uno que
llevaba tiempo declarado.

## Consecuencias positivas
- `SELECT enrollment_type FROM enrollments` distingue cómo se obtuvo cada acceso, que es la única
  razón por la que la columna existe.
- El lenguaje ubicuo y el código dejan de discrepar en §82.
- T23 conserva su texto y su fecha, y el alcance de su cláusula queda por escrito en vez de vivir en
  la interpretación de quien lea el código.
- El flujo gratuito no cambia: `EnrollStudentHandler` sigue produciendo `Free`.

## Consecuencias negativas
- Quien lea T23 sin leer este ADR puede creer que la implementación lo contradice. Se mitiga con la
  fila de T28 en el índice y con la referencia cruzada de esta sección.
- Cualquier consumidor que persista `enrollment_type` verá un valor que antes no podía aparecer.
  Hoy no existe ninguno fuera de Enrollment.

## Riesgos residuales
Que la precisión de esta decisión se use como precedente para abrir el agregado por otras vías. El
alcance está acotado a los siete puntos de arriba: cualquier otro cambio en `Matrícula` —una
invariante, la clave natural o el evento— sigue prohibido por T23 y exigiría su propio ADR.

## Decisiones relacionadas
[T13](./ADR-T13-saga-matricula-de-pago.md) · [T23](./ADR-T23-comando-matricula-de-pago.md) · [T03](./ADR-T03-arquitectura-limpia.md) · [T20](./ADR-T20-versionado-de-contratos.md)
