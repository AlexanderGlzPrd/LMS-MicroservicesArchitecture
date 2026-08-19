# ADR-0029 — Fuente provisional del nombre del estudiante en Certification

## Estado
Aceptado — 2026-08-16

## Contexto
`contextos-delimitados.md` §5 fija que el **Certificado nace solo con información completa** y que el
nombre del estudiante y el título del curso se **congelan al emitir**.
[ADR-0018](./ADR-0018-seguridad-con-keycloak.md) y `arquitectura-de-seguridad.md` §5 designan al propietario de ese
nombre: **Keycloak, consultado mediante Admin API con cuenta de servicio y un ACL mínimo** que extrae
únicamente el nombre visible. El endpoint `userinfo` quedó descartado —representa al usuario del
token presentado, no a un tercero identificado por un hecho asíncrono— y `matriz-de-comunicacion.md` §2
añade la regla de disponibilidad: si la fuente no responde, **no se emite; reintento posterior**.

`certification` se construye para cerrar el flujo `Learning → Certification`, y en ese momento
**Keycloak todavía no está desplegado**: realm `lms`, clientes, roles, validación de JWT en cada
servicio y Gateway ([ADR-0017](./ADR-0017-api-gateway-con-yarp.md)) llegan más tarde. Mientras tanto,
los tres servicios existentes derivan la identidad del estudiante de una cabecera `X-Student-Id`
provisional.

Certification queda por tanto con una exigencia y sin su fuente: debe congelar un nombre que hoy
ningún componente del sistema puede proporcionar.

## Problema
¿De dónde obtiene Certification el nombre visible del estudiante mientras Keycloak no existe, sin
adelantar su infraestructura, sin cambiar el propietario del dato y sin emitir certificados
incompletos?

## Alternativas consideradas
- **Transportar `StudentName` dentro de `CursoFinalizado`**: [ADR-0018](./ADR-0018-seguridad-con-keycloak.md) ya
  lo evaluó y lo **descartó** —Learning no posee ese dato ni debe transportar información ajena—.
  Adoptarlo exigiría revocar esa alternativa, no una decisión de implementación.
- **Adelantar Keycloak solo para el ACL de nombres**: arrastra realm, clientes de servicio y
  operación del proveedor de identidad, a cambio de un único atributo.
- **No emitir ningún certificado hasta que exista Keycloak**: Certification quedaría construida pero
  sin producir nada, y la verificación pública por `CertificateId` no tendría nada que verificar.
- **Nombre por defecto o marcador de posición** (`"desconocido"`, cadena vacía, el propio
  `StudentId`): contradice `contextos-delimitados.md` §5, porque congelaría en un artefacto **inmutable**
  un valor que no es el nombre de nadie.
- **Puerto definitivo con adaptador provisional detrás**: la frontera queda desde el primer día en su
  forma final y solo el adaptador es temporal.

## Decisión
Certification declara el **puerto definitivo** `IStudentDirectory` en `Certification.Application`, y
lo satisface hasta la llegada de Keycloak con un **adaptador provisional** alojado exclusivamente en
`Certification.Infrastructure`.

> El nombre visible del estudiante se obtiene **siempre** a través de `IStudentDirectory`. Hasta que
> se implemente la integración real con **Keycloak Admin API**, ese puerto lo satisface un adaptador
> provisional de `Certification.Infrastructure`, y **solo ese adaptador** se sustituye entonces.

**Contrato del puerto.** `IStudentDirectory` devuelve un resultado de **tres estados mutuamente
excluyentes**:

| Estado | Significado |
|---|---|
| `Resolved` | el nombre visible existe y es válido |
| `NotFound` | la fuente respondió y ese estudiante no existe en ella |
| `Unavailable` | la fuente no pudo consultarse |

**Reglas de la decisión:**

1. **`StudentName` sigue siendo propiedad de Identity.** El adaptador provisional **no** convierte a
   Certification en propietario ni en custodio del dato: sigue consumiéndolo a través de un ACL, y
   sigue sin recibir correo, roles, grupos, credenciales ni perfil completo.
2. **No existe nombre por defecto.** Ni `"desconocido"`, ni cadena vacía, ni el `StudentId` como
   sustituto. El resultado `Resolved` rechaza un nombre vacío o en blanco.
3. **No se emite ningún `Certificate` sin un nombre válido.** La factoría del agregado lo exige, y el
   camino de emisión solo la invoca con el nombre y el título resueltos.
4. **`NotFound` y `Unavailable` dejan la emisión pendiente.** Ninguno de los dos es terminal: no se
   descarta el trabajo, no se emite nada parcial y el reintento posterior sigue siendo automático.
   Se distinguen entre sí en el diagnóstico, no en el destino.
5. **La sustitución es obligatoria.** Al introducir la integración real con Keycloak Admin API, el
   adaptador provisional **se retira**. No se conserva como alternativa, ni como modo de
   configuración, ni como respaldo.
6. **La sustitución no toca `Domain` ni `Application`.** El puerto, su resultado de tres estados y
   los casos de uso que lo consumen permanecen idénticos: cambia un archivo de `Infrastructure` y su
   registro en la composición.
7. **No adelanta nada más.** Esta decisión **no** introduce JWT, ni validación de tokens, ni realm,
   ni clientes, ni roles, ni Gateway, ni cuenta de servicio. La cabecera `X-Student-Id` sigue siendo
   el mecanismo provisional de actor en los tres servicios que lo usan, y se retira con la
   introducción de la seguridad real.
8. **[ADR-0018](./ADR-0018-seguridad-con-keycloak.md) sigue vigente e íntegro.** Keycloak sigue siendo el
   proveedor de identidad, la Admin API con cuenta de servicio y ACL mínimo sigue siendo la fuente
   designada del nombre, `userinfo` sigue descartado y el nombre sigue sin viajar en
   `CursoFinalizado`. Esta decisión no sustituye ninguna de aquellas: describe el intervalo anterior
   a su implementación.
9. **Alcance acotado, sin precedente genérico.** Esta excepción se aplica **solo** a la resolución
   del nombre visible del estudiante en Certification. **No** autoriza adaptadores provisionales en
   ninguna otra integración: cualquier caso nuevo exige su propia decisión registrada, con sus
   propias razones, sin poder apoyarse en esta.

## Justificación
La exigencia de `contextos-delimitados.md` §5 llega antes que Keycloak, y solo hay dos formas de
resolver ese desfase: adelantar el proveedor de identidad o poner algo detrás del puerto. Adelantarlo
arrastra realm, clientes, roles y validación de token, todo ello para resolver un único atributo.

Poner el adaptador detrás del puerto tiene una propiedad que ninguna otra alternativa ofrece: **lo
provisional queda confinado en el único sitio que la arquitectura permite sustituir sin efectos**.
`Certification.Domain` y `Certification.Application` no llegan a conocer la diferencia, así que la
integración real no es una migración sino un reemplazo de adaptador, que es exactamente la propiedad
que [ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) busca.

Los tres estados tampoco son un detalle de implementación. Un booleano obligaría a tratar igual «ese
estudiante no existe» y «no se pudo consultar», que son dos diagnósticos distintos. Y la prohibición
de nombre por defecto protege una invariante: un certificado es **inmutable**, de modo que un nombre
inventado ya no se corrige.

## Consecuencias positivas
- El flujo completo (matricular, progresar, finalizar, certificar, verificar) queda operativo sin
  adelantar la infraestructura de seguridad.
- La frontera con Identity queda desde el primer día en su forma definitiva.
- La sustitución futura es un cambio de un archivo, verificable por inspección.
- Ningún certificado puede nacer con un nombre que no sea el del estudiante.

## Consecuencias negativas
- Existe un adaptador que hay que recordar retirar, y una configuración de Development que hay que
  sembrar para los estudiantes de prueba.
- Un estudiante ausente de esa configuración no obtiene certificado hasta que llegue Keycloak,
  aunque su Finalización esté sellada. Su emisión queda pendiente, no perdida.

## Riesgos residuales
Que el adaptador provisional sobreviva a la introducción de Keycloak por olvido. Se mitiga con la
obligación de retirada declarada arriba, con el nombre explícito del archivo y con el alcance
acotado del punto 9, que impide que la figura se reutilice en otras integraciones.

## Decisiones relacionadas
[ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) · [ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md) · [ADR-0017](./ADR-0017-api-gateway-con-yarp.md) · [ADR-0018](./ADR-0018-seguridad-con-keycloak.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md)
