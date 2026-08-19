# ADR-0033 — Continuidad del contexto de traza en los saltos asíncronos

## Estado
Aceptado — 2026-08-19

## Contexto
[ADR-0019](./ADR-0019-observabilidad.md) exige correlación propagada **tanto en HTTP como en mensajería**, y
nombra tres flujos que deben poder seguirse completos: matrícula gratuita hasta la creación del
progreso, finalización de curso hasta la emisión del certificado, y cada estado y compensación de la
Saga de compra.

Con OpenTelemetry instalado, la parte HTTP se resuelve sola: el contexto W3C viaja en cabeceras y el
árbol de spans se encadena sin escribir código. La mensajería tiene un corte que la instrumentación
no puede cerrar por sí misma, porque el mensaje **no se publica dentro de la actividad que lo
originó**.

Hay **dos** cortes, no uno:

1. **El Outbox.** La transacción de negocio escribe una fila; minutos después —o tras un reinicio— un
   servicio alojado la lee y la publica. En ese momento `Activity.Current` es nula o pertenece al
   ciclo del despachador. El span productor nace huérfano y aparecen dos árboles inconexos por cada
   salto: uno que muere al responder y otro que aparece de la nada al publicar.
2. **El arranque de la Saga.** `POST /api/v1/purchases` no produce ningún mensaje: crea la compra y
   responde `202`. El primer trabajo lo inicia el conductor más tarde, en su propio ciclo, igual de
   desprovisto de contexto. Cerrar solo el primer corte mantiene la traza **a partir de que un
   mensaje existe**, pero el primer mensaje de la compra ya nace bajo una raíz nueva: la petición del
   estudiante queda en un árbol y toda la Saga en otro.

## Problema
¿Cómo se conserva el contexto de traza a través de un salto asíncrono sin tocar el contrato del
mensaje, el sobre publicado, la topología del broker ni el estado del dominio?

## Alternativas consideradas
- **No hacer nada y aceptar árboles partidos.** Incumple ADR-0019 justo en el punto que ADR-0019 nombra: la
  correlación en mensajería.
- **Meter el `traceparent` en el `Payload` del mensaje.** Convierte metadato de telemetría en parte
  del contrato: obliga a versionar los eventos y a que el consumidor conozca un campo que no es suyo.
  Contradice [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md).
- **Añadir una cabecera propia al sobre publicado.** El transporte ya emite las cabeceras W3C por su
  cuenta; el problema no es el transporte, es que en el momento de publicar no hay contexto vivo que
  emitir.
- **Reconstruir el contexto por correlación de identificadores en el visor de trazas.** Depende del
  visor, no del sistema, y no produce un árbol: produce una búsqueda manual.
- **Persistir el contexto como metadato técnico junto al trabajo pendiente**, y restaurarlo justo
  antes de publicar o de avanzar.

## Decisión
Se persiste el contexto W3C —el valor de `traceparent`— como **metadato técnico**, en los dos puntos
donde un salto asíncrono rompe la traza, y se restaura antes de producir el trabajo siguiente.

**(a) Outbox.** Columna `trace_context text NULL` en las cuatro tablas `outbox_messages`
—`enrollment`, `learning`, `paid-enrollment` y `payment-provider-sim`—.

1. **Al escribir.** El escritor guarda el `traceparent` de la actividad vigente en la **misma
   transacción local** que el mensaje y que el cambio de negocio. Es una escritura más en una fila que
   ya se estaba escribiendo: ni una consulta extra, ni una transacción extra.
2. **Al publicar.** El despachador reconstruye el contexto e inicia el span productor **como hijo** de
   él. A partir de ahí el bus propaga sus cabeceras con normalidad y el consumidor continúa el árbol
   sin cambio alguno.

**(b) Arranque de la Saga.** Columna `trace_context text NULL` en `purchases`, declarada como
**propiedad sombra** de EF Core.

3. **La escribe Infrastructure, y solo Infrastructure.** Un `SaveChangesInterceptor` registrado
   únicamente en el contexto de persistencia de `paid-enrollment` detecta las compras en estado
   `Added` y asigna la propiedad antes de persistir, participando en la transacción que ya estaba
   abierta.
4. **La lee Infrastructure.** El conductor la lee y restaura el contexto **antes de iniciar su primer
   trabajo y antes de producir el primer mensaje**. Ese mensaje hereda entonces el contexto correcto
   por la vía normal de (a), y todos los saltos posteriores se encadenan solos.
5. **El dominio no la ve.** No existe propiedad, campo, constructor ni método nuevo en el agregado
   `Purchase`, y ninguna invariante la conoce. La capa de aplicación no menciona EF, ni actividades,
   ni telemetría.

**Reglas comunes a los dos puntos:**

6. **La telemetría nunca bloquea.** Valor ausente, nulo o inválido —incluidas las filas y las compras
   anteriores a la migración— produce **actividad raíz nueva más advertencia**. No se detiene la
   publicación, no se reintenta, no se marca nada como fallido y no se altera ninguna transición.
7. **Nada más cambia.** Ni el `Payload` ni su serialización, ni el sobre publicado, ni los contratos,
   ni los exchanges, ni las routing keys, ni el Inbox, ni la deduplicación por identificador de
   mensaje, ni la máquina de estados, ni las compensaciones.
8. **Lo persistido es metadato técnico**, del mismo orden que el número de intentos o el último error
   que ya conviven en la tabla del Outbox sin ser parte de ningún contrato.

Son cinco columnas en cinco tablas, todas anulables y sin valor por defecto.

## Justificación
El contexto de traza describe **cuándo ocurrió el trabajo**, no **qué se pide**. Guardarlo junto al
trabajo pendiente lo mantiene donde es útil —en la fila que alguien leerá después— sin ascenderlo a
contrato, que es lo que haría meterlo en el `Payload`.

La forma de (a) se apoya en una propiedad que [ADR-0011](./ADR-0011-outbox-transaccional.md) ya garantiza:
si el mensaje y el cambio de negocio se escriben en la misma transacción, el contexto escrito con
ellos tiene exactamente la misma atomicidad, sin coste añadido.

La forma de (b) existe porque la compra es el único caso donde media un conductor entre la petición y
el primer mensaje. Resolverlo con una propiedad sombra mantiene la columna en infraestructura, que es
donde vive el problema: el dominio no gana estado que no significa nada para él, y
[ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) se conserva intacta.

La ausencia de contexto degrada a raíz nueva y nunca a error, para que la observabilidad no se
convierta en un modo de fallo del sistema observado.

## Consecuencias positivas
- Los tres flujos que ADR-0019 exige seguir completos se ven como **una sola traza**, con la petición del
  usuario como raíz.
- La compra se ve entera desde el `POST` del estudiante, no desde el conductor.
- El diagnóstico de un fallo distribuido deja de exigir cruzar identificadores manualmente entre
  consolas.
- Ni los contratos ni el dominio aprenden nada de telemetría.

## Consecuencias negativas
- Cinco columnas nuevas sobre tablas vivas, que crecen con las filas que las contienen.
- Las cuatro implementaciones de Outbox repiten el mismo par escribir/restaurar.
- Una columna de `purchases` es invisible desde el dominio y puede confundirse con estado de negocio
  al mirar la tabla.

## Riesgos residuales
Que alguien lea `purchases.trace_context` como dato de negocio, o que lo use para decidir algo. La
mitigación es su naturaleza: se declara solo en la configuración de persistencia, la escribe un
interceptor que la capa de aplicación no ve, y ninguna consulta la lee salvo el conductor.

Retención: la columna del Outbox crece con `outbox_messages` y se purgará con ellas cuando exista una
política de retención de tablas técnicas.

## Decisiones relacionadas
[ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) · [ADR-0011](./ADR-0011-outbox-transaccional.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md) · [ADR-0016](./ADR-0016-saga-de-compra-de-acceso.md) · [ADR-0019](./ADR-0019-observabilidad.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) · [ADR-0025](./ADR-0025-building-blocks-tecnicos.md)
