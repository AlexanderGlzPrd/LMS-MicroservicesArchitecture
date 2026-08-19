# ADR-0032 — Consulta de acceso de Enrollment autorizada por cuenta de servicio

## Estado
Aceptado — 2026-08-19

## Contexto
`matriz-de-comunicacion.md` §2 clasifica `ConsultarAcceso` —el pre-check que hace `paid-enrollment`
antes de cobrar— como **consulta síncrona que exige frescura inmediata**, y §7 la considera
precondición verificable. `flujos-de-aplicacion.md` §2 ya nombra a `paid-enrollment` como iniciador
de esa consulta.

Quien la inicia, sin embargo, **no es el titular del recurso consultado**. `PurchaseAdvancer` la
ejecuta desde `PurchaseDriver`, un `BackgroundService` que despierta por temporizador: no hay
petición HTTP entrante, no hay token de usuario y no existe nada que propagar. Hasta ahora el hueco
se tapaba suplantando al estudiante con la cabecera `X-Student-Id`, que ningún componente firmaba.

Con validación real de JWT esa suplantación deja de ser posible, y
`arquitectura-de-seguridad.md` §2 fija el mecanismo que la sustituye: comunicación máquina a máquina
por `client_credentials` con **audiencia específica del destino**. Lo que ninguna decisión previa
resuelve es **cómo autoriza Enrollment una lectura cuyo iniciador no es el estudiante**.

## Problema
¿Cómo consulta `paid-enrollment` si un estudiante ya tiene acceso a un curso, sin token interactivo
de ese estudiante, sin fabricar su identidad y sin abrir una ruta que un cliente externo pueda
alcanzar?

## Alternativas consideradas
- **Conservar la suplantación por cabecera.** Contradice `arquitectura-de-seguridad.md` §3 y deja el
  consentimiento de [ADR-0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md) apoyado en un dato sin firmar.
- **Convertir la consulta en mensaje.** [ADR-0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md) resolvió así el
  caso gemelo —la **concesión** de matrícula—, pero la **consulta** exige frescura inmediata y una
  respuesta que decide si se cobra. Un intercambio petición/respuesta sobre el broker reintroduce
  acoplamiento temporal sin ninguna de las garantías que justifican la mensajería.
- **Token exchange o identidad delegada.** Es la figura de grado productivo, y `arquitectura-de-seguridad.md` §6
  ya la documenta como evolución. Exige emisor de identidad delegada y ciclo de vida propio, para una
  única lectura de solo lectura.
- **Congelar el resultado del pre-check en el `Purchase`.** El dato dejaría de ser fresco justo donde
  la matriz exige frescura, y una matrícula concedida entre la comprobación y el cobro pasaría
  inadvertida.
- **Endpoint interno autorizado por rol de cuenta de servicio.** Mantiene la consulta síncrona, no
  inventa identidad y hace explícito quién puede preguntar.

## Decisión
Enrollment expone una consulta de solo lectura, alcanzable **únicamente** por cuentas de servicio:

```
GET /api/v1/enrollments/access?studentId={guid}&courseId={guid}
Authorization: Bearer <token de client_credentials>
```

**Reglas de la decisión:**

1. **Autenticación por `client_credentials`.** El llamante es la cuenta de servicio de
   `paid-enrollment-svc`. Enrollment exige su **audiencia propia**, `enrollment-api`, como en
   cualquier otro token que acepta.
2. **Autorización por rol de cliente.** La ruta exige el rol `access-reader`, **propiedad del cliente
   `enrollment-api`** y asignado en exclusiva a esa cuenta de servicio. Los clientes que emiten
   tokens de persona no tienen ese rol, así que **ningún token de usuario puede portarlo**.
3. **`studentId` es un parámetro, no una identidad.** Enrollment lo trata como argumento de una
   consulta ya autorizada por rol de servicio. **No es identidad autenticada** y no concede nada por
   sí mismo. La regla de `arquitectura-de-seguridad.md` §3 sigue intacta para todo lo demás: esta es
   su única excepción declarada, y es de lectura.
4. **Fuera de la superficie pública.** La ruta **no se declara en el Gateway**. Una petición a ella
   desde fuera muere con `404` antes de llegar a la autorización, **con token válido y sin él**: la
   exclusión es estructural, por forma del patrón de rutas, no una regla de permisos que alguien
   pueda relajar.
5. **Solo lectura.** No crea, no modifica y no concede matrículas. La única apertura de escritura de
   Enrollment sigue siendo la de [ADR-0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md): por mensajería, con
   ledger y con permisos de broker.
6. **Semántica de respuesta conservada.** `200` matriculado · `404` no matriculado · cualquier otra
   respuesta o excepción de resiliencia → `Unknown`. El ACL de `paid-enrollment` y su traducción no
   cambian, y el pipeline `timeout → retry → circuit breaker` de
   [ADR-0022](./ADR-0022-politicas-de-resiliencia.md) sigue gobernando el tiempo.
7. **El caso de uso no conoce al actor.** Su handler recibe `StudentId` y `CourseId` como parámetros
   y **no depende de `ICurrentActor`**, precisamente porque el estudiante consultado no es quien
   ejecuta.
8. **Alcance acotado, sin precedente genérico.** Esta figura autoriza **solo** la consulta de acceso
   de Enrollment. Cualquier otra lectura por identidad técnica exige su propia decisión registrada.

## Justificación
El caso tiene una asimetría que ninguna decisión previa cubría: el iniciador legítimo de una consulta
no siempre es el titular del dato consultado. Tratarlo como identidad delegada exige maquinaria que
el MVP no tiene; tratarlo como suplantación destruye la propiedad que hace creíble el consentimiento
del `Purchase`.

Autorizar por **rol de cuenta de servicio** nombra el hecho tal como es: *este servicio, y ningún
otro, puede preguntar por el acceso de cualquier estudiante*. La contención no depende de esconder la
ruta, sino de tres propiedades comprobables por inspección: el rol vive en un cliente que no se
asigna a los clientes de persona, la ruta no existe en la lista blanca del borde, y la operación no
escribe nada.

Que la exclusión del Gateway sea por **forma del patrón** y no por un `Deny` importa: un permiso se
relaja con una línea, mientras que declarar la ruta exige escribirla entera y verla en revisión.

## Consecuencias positivas
- Desaparece la última suplantación de identidad del sistema.
- El pre-check conserva frescura inmediata y su traducción a `Unknown`, con la misma resiliencia.
- Quién puede preguntar queda escrito en un rol y verificable en el token, no en una convención.
- `Domain` y `Application` de Enrollment no aprenden nada sobre tokens: cambia un controlador y una
  política.

## Consecuencias negativas
- Enrollment gana una ruta que no pertenece a su superficie pública y que hay que recordar mantener
  fuera de ella.
- La cuenta de servicio puede consultar el acceso de **cualquier** estudiante, no solo el de la
  compra en curso. Es el precio de no tener identidad delegada.

## Riesgos residuales
Que alguien declare la ruta en el Gateway por conveniencia y la exponga, o que el rol `access-reader`
se asigne a un cliente de persona. Lo primero se detecta leyendo un archivo y se comprueba con la
respuesta `404` ante credenciales válidas de los tres roles; lo segundo, comprobando que un token de
usuario no contiene ese rol.

## Decisiones relacionadas
[ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0017](./ADR-0017-api-gateway-con-yarp.md) · [ADR-0018](./ADR-0018-seguridad-con-keycloak.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md) · [ADR-0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md) · [ADR-0027](./ADR-0027-versionado-de-apis-rest.md)
