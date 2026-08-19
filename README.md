# LMS — Plataforma de aprendizaje basada en microservicios

Plataforma de formación donde un instructor crea cursos con lecciones, los publica y un público
general los consulta en un catálogo; un estudiante puede matricularse gratuitamente en los cursos
publicados y avanzar por ellos hasta finalizarlos; también puede comprar el acceso a un curso de
pago. El sistema está repartido en servicios independientes escritos en .NET, cada uno con su base
de datos y su propio ciclo de despliegue.

El repositorio contiene seis microservicios ejecutables, un BFF de composición y un gateway de
entrada:

| Unidad | Puerto | Qué hace |
|---|---|---|
| **Course Authoring** | `5195` | autoría de cursos, publicación y catálogo público |
| **Enrollment** | `5196` | matrícula de un estudiante en un curso publicado, gratuita o concedida por una compra |
| **Learning** | `5197` | lecciones completadas de un estudiante y finalización del curso |
| **Certification** | `5198` | emisión del certificado y su verificación pública |
| **BFF de composición** | `5199` | una sola respuesta con el progreso del estudiante y los datos del catálogo |
| **Paid Enrollment** | `5200` | compra de acceso a un curso, coordinada como una Saga con compensaciones |
| **Payment Provider Sim** | `5201` | proveedor de pago simulado; solo expone `/health` |
| **Gateway** | `5202` | entrada única: valida el token, autoriza por ruta y reenvía a su destino |

Cada uno de los servicios tiene su propia base de datos y su propio usuario de PostgreSQL,
sin acceso a las bases de los demás. Enrollment no lee la base de Course Authoring: le pregunta por
HTTP si el curso está publicado antes de conceder una matrícula, y si no obtiene respuesta no
matricula a nadie. Learning hace lo mismo con el contenido: antes de registrar cualquier lección
pregunta a Course Authoring cuáles son las lecciones publicadas del curso, y si no puede saberlo no
escribe nada. El BFF no tiene base de datos ni la necesita: no persiste nada y solo consume las API
públicas de Learning y Course Authoring.

Matricularse y empezar un curso están conectados por un mensaje, no por una llamada: cuando
Enrollment concede una matrícula, publica el hecho en RabbitMQ y Learning lo consume para crear el
progreso del estudiante. Learning nunca pregunta a Enrollment. Eso significa que el progreso tarda
un momento en aparecer, y que la matrícula se concede aunque el broker esté apagado.

La finalización de un curso es irreversible: cuando el estudiante completa todas las lecciones
publicadas, Learning la sella con su fecha y ninguna operación posterior la deshace ni la reescribe.

Certificar funciona igual que empezar un curso: al sellar la finalización, Learning publica el hecho
y Certification lo consume. El certificado no nace en ese instante, porque su nombre visible y el
título del curso no viajan en el mensaje: Certification anota el trabajo pendiente y lo resuelve
después, cuando puede preguntar por los dos datos. Si alguno no está disponible, el trabajo espera y
se reintenta solo; nunca se emite un certificado a medias.

Learning mantiene además dos modelos separados de los mismos datos: uno para escribir, que protege
las reglas del progreso, y otro para leer, que responde las consultas ya resuelto e incluye el
porcentaje de avance. El segundo se actualiza a partir del primero, así que va un momento por detrás.

Un curso mantiene dos contenidos a la vez: el **contenido de trabajo**, que solo ve su instructor y
cambia con cada edición, y el **contenido publicado**, que ve el público en el catálogo y solo cambia
al publicar o republicar.

Comprar el acceso a un curso es lo único que no encaja en ese flujo, y por eso vive aparte.
Matricularse, avanzar, finalizar y certificar encadenan hechos que no se deshacen: revocar un
certificado legítimamente ganado no sería compensar nada, sería falsearlo. Una compra sí tiene
marcha atrás real —se puede anular una autorización antes de cobrar y devolver el dinero después—,
así que Paid Enrollment la coordina paso a paso: comprueba que el estudiante no tenga ya acceso,
autoriza el pago, lo captura, pide a Enrollment que conceda la matrícula y solo entonces da la
compra por confirmada. Si algo falla por el camino, deshace lo que hizo: anula si todavía no había
cobrado, reembolsa si ya había cobrado. Y cuando emite una operación y no llega la respuesta, no
supone nada: pregunta al proveedor cuál fue el resultado real antes de decidir. Si ni así puede
saberlo, deja la compra suspendida a la espera de que una persona la resuelva, en vez de arriesgar
un cobro o una devolución que nadie ha confirmado.

## Tecnologías

- .NET 10 · ASP.NET Core (controladores)
- Entity Framework Core 10 · Npgsql · PostgreSQL 17
- Llamada síncrona entre servicios con `HttpClient` y timeout acotado
- RabbitMQ 4 y MassTransit para los eventos de integración y los mensajes de la Saga de compra
- Versionado de API con `Asp.Versioning`
- OpenAPI + Scalar para documentación interactiva
- Docker Compose para la base de datos y el broker locales
- xUnit y Testcontainers para las pruebas

## Requisitos

| Requisito | Versión | Para qué |
|---|---|---|
| .NET SDK | 10.0.302 o superior dentro de 10.0 (fijada en `global.json`) | compilar y ejecutar |
| Docker Desktop | reciente | PostgreSQL, RabbitMQ y Keycloak locales, y pruebas de integración |
| `dotnet-ef` | 10.x | aplicar migraciones |

```bash
dotnet --list-sdks                        # debe aparecer un 10.0.x
dotnet tool install --global dotnet-ef    # o: dotnet tool update --global dotnet-ef
```

> Si abres la solución en Visual Studio, necesitas Visual Studio 2026: .NET 10 exige MSBuild 18.x.
> La compilación por línea de comandos funciona igualmente.

## Instalación

```bash
git clone https://github.com/AlexanderGlzPrd/LMS-MicroservicesArchitecture.git
cd LMS-MicroservicesArchitecture
dotnet restore LMS.sln
dotnet build LMS.sln
```

Todos los comandos siguientes se ejecutan desde la raíz del repositorio.

## Ejecución

Hay dos formas de levantar el sistema. La **recomendada** es Docker Compose: un comando, dieciséis
contenedores y el esquema aplicado antes de que arranque ninguna API. La segunda —arrancar los ocho
procesos a mano con `dotnet run`— sigue funcionando igual que siempre y está descrita a partir del
paso 1.

### Todo el sistema con Docker Compose

```bash
docker compose build     # la primera vez tarda: nueve imágenes desde cero
docker compose up -d
docker compose ps        # quince contenedores en marcha y `lms-migrator` en `exited (0)`
```

El `.env` versionado fija `COMPOSE_PROFILES=core,payments,observability`, así que `up` levanta el
stack íntegro. Es un archivo de **desarrollo local**: sus tres secretos son los mismos que ya viajan
en claro en `deploy/keycloak/realm-lms.json` y no sirven para nada fuera de esta máquina. Para
personalizar valores sin tocarlo, `.env.local` sigue ignorado por git:

```bash
docker compose --env-file .env --env-file .env.local up -d
```

**Perfiles.** Cuatro servicios no tienen perfil y arrancan siempre: `postgres`, `rabbitmq`,
`keycloak` y `migrator`. El resto se agrupa en tres:

| Perfil | Contenedores | Para qué |
|---|---|---|
| `core` | `course-authoring`, `enrollment`, `learning`, `certification`, `bff-composition`, `gateway` | El recorrido gratuito completo |
| `payments` | `paid-enrollment`, `payment-provider-sim` | La compra de acceso y su Saga |
| `observability` | `otel-collector`, `jaeger`, `prometheus`, `grafana` | Trazas, métricas y dashboards |

```bash
docker compose up -d                                   # los dieciséis
COMPOSE_PROFILES=core docker compose up -d             # infraestructura y flujo gratuito
COMPOSE_PROFILES=core,payments docker compose up -d    # todo el dominio, sin observabilidad
```

Ningún servicio depende del arranque de otro perfil, así que cualquiera de las tres combinaciones es
válida por sí sola. Con `payments` apagado, las rutas de compra del Gateway **siguen declaradas** y
fallan por destino ausente, no con `404`.

**El migrador.** `lms-migrator` es un contenedor de un solo uso: aplica las seis migraciones en orden
y termina. Las ocho unidades esperan a que acabe con éxito, así que un esquema a medias no arranca
nada. Es idempotente: sobre bases al día no hace nada y sale con código 0.

**Puertos**, todos publicados solo en `127.0.0.1`:

| Contenedor | Puerto | Qué es |
|---|---|---|
| `lms-gateway` | `5202` | **Entrada única del sistema** |
| `lms-course-authoring` · `lms-enrollment` · `lms-learning` · `lms-certification` | `5195`–`5198` | Inspección directa de cada API |
| `lms-bff-composition` · `lms-paid-enrollment` · `lms-payment-provider-sim` | `5199`–`5201` | BFF, Saga y proveedor simulado |
| `lms-keycloak` | `8080` | Consola y emisión de tokens |
| `lms-postgres` · `lms-rabbitmq` | `5432` · `5672` y `15672` | Bases y broker, con su consola de gestión |
| `lms-jaeger` · `lms-prometheus` · `lms-grafana` | `16686` · `9090` · `3000` | Trazas, métricas y dashboards |

El Collector (`4317`, `4318`, `8889`) y el puerto de métricas del broker (`15692`) **no se publican**:
se usan solo por nombre DNS dentro de la red.

**Observabilidad.** Con el perfil `observability` levantado:

- Trazas en <http://localhost:16686>. Una matrícula, una certificación o una compra completa se ven
  como **una sola traza**, desde la petición del usuario hasta el último consumidor.
- Métricas en <http://localhost:9090>, con dos objetivos: el Collector y el broker.
- Dashboards en <http://localhost:3000> (`admin` / `admin`): salud de los servicios, latencia y
  errores HTTP, mensajería y Saga. Se aprovisionan solos desde `deploy/grafana/`; **un panel editado
  a mano no sobrevive** a recrear el contenedor, porque la fuente de verdad es el archivo.

**Reinicio limpio.**

```bash
docker compose down      # para los contenedores y CONSERVA los tres volúmenes
docker compose down -v   # DESTRUCTIVO: borra las seis bases, el estado del broker y las series
```

`down -v` es también el único comando que vuelve a disparar los scripts de `deploy/postgres/init/` y
la importación de definiciones del broker, que solo corren con el directorio de datos vacío. Después
hay que rehacer el recorrido completo desde cero.

### Diagnóstico

```bash
docker compose logs -f gateway              # logs JSON de un servicio
docker compose logs migrator                # por qué no arranca nada: la migración que falló
docker compose logs | grep <traceId>        # una interacción entera, a través de los ocho procesos
```

Cada línea de petición lleva `TraceId`, `SpanId`, `CorrelationId` y `ServiceName` en su ámbito. El
`X-Correlation-Id` lo genera el Gateway si el cliente no lo envía, lo conserva si lo envía, y lo
devuelve en la respuesta.

**`X-Correlation-Id` viaja por HTTP y solo por HTTP.** El log de un consumidor de mensajes **no lo
lleva**, y eso es lo esperado: al cruzar el broker la correlación la sostienen el `TraceId` —continuo
gracias al contexto persistido en el Outbox—, el `MessageId`, el `CausationId` cuando existe y el
`PurchaseId`. No lo busques en el log de un consumidor.

Síntomas frecuentes:

| Síntoma | Dónde mirar |
|---|---|
| Ninguna unidad arranca | `docker compose logs migrator`: si terminó con error, el esquema está a medias |
| `401` en todo, con token válido | Keycloak todavía no ha importado el realm o el servicio no ha podido descargar sus claves |
| Una cola crece sin parar | Panel de mensajería: consumidores a cero significa que nadie consume; consumidores vivos, que van lentos |
| Una compra parada | Buscar en Jaeger por `lms.purchase.id`, y el estado en `GET /api/v1/purchases/{id}` |

### Ejecución manual con `dotnet run`

Los ocho procesos siguen arrancando desde el host sin ninguna variable adicional más allá de los dos
secretos de cuenta de servicio. Es la ruta útil para depurar con el IDE: se levanta solo la
infraestructura en contenedores y el código corre fuera.

### 1. Levantar PostgreSQL, RabbitMQ y Keycloak

```bash
docker compose down -v                                  # obligatorio si ya tenías el volumen de una versión anterior
docker compose up -d postgres rabbitmq keycloak         # solo la infraestructura, sin los ocho procesos
docker compose ps                                       # esperar STATUS = healthy en los tres contenedores
```

Levanta tres contenedores:

| Contenedor | Puertos | Qué es |
|---|---|---|
| `lms-postgres` | `5432` | PostgreSQL 17 |
| `lms-rabbitmq` | `5672` (AMQP) · `15672` (interfaz de gestión) | RabbitMQ 4 |
| `lms-keycloak` | `8080` | Keycloak 26 con el realm `lms` importado |

La interfaz de gestión del broker está en <http://localhost:15672> con usuario `lms` y contraseña
`lms`. Desde ahí se ven el exchange, la cola, cuántos mensajes hay pendientes y la cola de errores,
sin escribir una sola consulta.

`lms` es solo la cuenta de esa consola. Cada servicio se conecta al broker con **su propio usuario**
—`enrollment`, `learning`, `certification`, `paid-enrollment` y `payment-provider-sim`—, con
permisos acotados por expresión regular a sus propios exchanges y colas más los ajenos que
estrictamente necesita. Publicar en el exchange de comandos de la Saga solo lo puede hacer
`paid-enrollment`.

Esas credenciales, los exchanges, las colas y sus enlaces salen de
`deploy/rabbitmq/definitions.json`, que el broker importa al arrancar. Es la única fuente de su
configuración inicial: el Compose no declara usuario ni contraseña por su cuenta. El archivo
documenta cómo regenerar el hash de la contraseña si quieres cambiarla.

Keycloak importa el realm `lms` desde `deploy/keycloak/realm-lms.json` en cada arranque, y **no
tiene volumen**: al recrear el contenedor vuelve a quedar exactamente como describe ese archivo, que
es su única fuente de verdad. Cualquier ajuste se hace ahí, no en la consola. Su panel está en
<http://localhost:8080> con usuario `admin` y contraseña `admin`.

El realm trae tres roles —`Student`, `Instructor`, `Administrator`— y cuatro usuarios de
demostración cuyo identificador **es** el que ya usan los datos existentes, de modo que un token
recién emitido apunta a los cursos y matrículas que ya estaban ahí:

| Usuario | Contraseña | Rol | Identificador |
|---|---|---|---|
| `instructor` | `instructor` | `Instructor` | `11111111-1111-1111-1111-111111111111` |
| `estudiante1` | `estudiante1` | `Student` | `22222222-2222-2222-2222-222222222222` |
| `estudiante2` | `estudiante2` | `Student` | `99999999-9999-4999-8999-999999999999` |
| `administrador` | `administrador` | `Administrator` | `33333333-3333-3333-3333-333333333333` |

Son credenciales **de desarrollo**, igual que las contraseñas de PostgreSQL y del broker: existen
para el recorrido local y no se reutilizan fuera de él.

Los mensajes de la Saga viajan por dos exchanges **topic**, `lms.saga.commands` y `lms.saga.replies`,
con una routing key por mensaje. Son topic y no fanout porque cada uno transporta varios tipos hacia
consumidores distintos: si fueran fanout, el simulador de pagos recibiría el comando de conceder
matrícula y Enrollment recibiría los comandos de pago.

PostgreSQL, en su primer arranque, ejecuta los scripts de `deploy/postgres/init/`, que crean:

| Base | Usuario de servicio | Contraseña local |
|---|---|---|
| `course_authoring` | `course_authoring_user` | `course_authoring_dev` |
| `enrollment` | `enrollment_user` | `enrollment_dev` |
| `learning` | `learning_user` | `learning_dev` |
| `certification` | `certification_user` | `certification_dev` |
| `purchase` | `purchase_user` | `purchase_dev` |
| `payments` | `payments_user` | `payments_dev` |

Los scripts revocan el permiso de conexión que PostgreSQL concede por defecto a todos los roles,
así que cada usuario de servicio solo alcanza su propia base:

```bash
docker exec -e PGPASSWORD=enrollment_dev lms-postgres \
  psql -U enrollment_user -d course_authoring -c "SELECT 1"
# FATAL: permission denied for database "course_authoring"
```

> **`docker compose down -v` es necesario, no opcional**, si ya habías levantado el proyecto antes:
> los scripts de inicialización solo se ejecutan con el directorio de datos vacío, y la creación de
> la base `certification` y la revocación de permisos son parte de esa inicialización. Sin recrear el
> volumen, el cuarto servicio no tiene dónde conectarse. Recrear el volumen borra los datos locales
> de las cuatro bases, así que después hay que volver a aplicar las migraciones de los cuatro.
>
> Lo mismo vale para el broker: el usuario `lms` y la topología se importan con el volumen vacío. Si
> cambias `deploy/rabbitmq/definitions.json`, `docker compose down -v && docker compose up -d` es lo
> que vuelve a dejar el usuario y las colas en su sitio.

### 2. Aplicar las migraciones de los seis servicios

Paso obligatorio: las API no aplican migraciones al arrancar.

```bash
dotnet ef database update --project src/services/course-authoring/CourseAuthoring.Infrastructure
dotnet ef database update --project src/services/enrollment/Enrollments.Infrastructure
dotnet ef database update --project src/services/learning/Learning.Infrastructure
dotnet ef database update --project src/services/certification/Certification.Infrastructure
dotnet ef database update --project src/services/paid-enrollment/PaidEnrollment.Infrastructure
dotnet ef database update --project src/services/payment-provider-sim/PaymentProviderSim.Worker
```

Un comando por servicio, no por migración: Learning tiene varias y `database update` las aplica
todas en orden hasta dejar la base al día. El simulador de pagos no tiene proyecto de infraestructura
aparte: su `DbContext` vive en el propio Worker.

Cada fábrica de tiempo de diseño usa por defecto su cadena de conexión local. Para apuntar a otra
base, define la variable correspondiente:

```bash
# PowerShell
$env:COURSE_AUTHORING_CONNECTION = "Host=...;Database=...;Username=...;Password=..."
$env:ENROLLMENT_CONNECTION       = "Host=...;Database=...;Username=...;Password=..."
$env:LEARNING_CONNECTION         = "Host=...;Database=...;Username=...;Password=..."
$env:CERTIFICATION_CONNECTION    = "Host=...;Database=...;Username=...;Password=..."
$env:PURCHASE_CONNECTION         = "Host=...;Database=...;Username=...;Password=..."
$env:PAYMENTS_CONNECTION         = "Host=...;Database=...;Username=...;Password=..."
```

### 3. Arrancar los servicios y el BFF

Cada uno en su propia terminal:

```bash
dotnet run --project src/services/course-authoring/CourseAuthoring.Api --launch-profile http
dotnet run --project src/services/enrollment/Enrollments.Api --launch-profile http
dotnet run --project src/services/learning/Learning.Api --launch-profile http
dotnet run --project src/services/certification/Certification.Api --launch-profile http
dotnet run --project src/bff/BffComposition.Api --launch-profile http
dotnet run --project src/services/paid-enrollment/PaidEnrollment.Api --launch-profile http
dotnet run --project src/services/payment-provider-sim/PaymentProviderSim.Worker --launch-profile http
dotnet run --project src/gateway/Gateway.Api --launch-profile http
```

Escuchan en `http://localhost:5195` a `http://localhost:5202`, con
`ASPNETCORE_ENVIRONMENT=Development`. El BFF solo hace falta para la vista compuesta, y las dos
unidades de pago solo para comprar acceso a un curso; el flujo gratuito funciona sin ellas.

Dos de ellas necesitan el secreto de su cuenta de servicio y **no arrancan sin él**. Ese secreto no
está en ningún `appsettings`, aunque sí viaja en claro —igual que los otros dos de desarrollo— en
`deploy/keycloak/realm-lms.json` y en el `.env` versionado: son credenciales de esta máquina y de
nada más, y no deben reutilizarse en ningún entorno accesible desde fuera de ella:

```bash
export Keycloak__ClientSecret=certification-svc-dev-secret
dotnet run --project src/services/certification/Certification.Api --launch-profile http

export Services__Enrollment__ClientSecret=paid-enrollment-svc-dev-secret
dotnet run --project src/services/paid-enrollment/PaidEnrollment.Api --launch-profile http
```

Certification lo usa para preguntar a Keycloak el nombre visible del estudiante antes de emitir un
certificado; `paid-enrollment`, para consultar en Enrollment si el estudiante ya tenía acceso antes
de cobrarle. En ambos casos es una cuenta de servicio con un único permiso, no una identidad de
persona.

El gateway es la entrada única del sistema: expone en `5202` las rutas declaradas en su
`appsettings.json` y **nada más**. Una ruta que no esté en esa lista responde `404` aunque exista en
el servicio de detrás, y lo mismo ocurre con un verbo no declarado. Los puertos de cada servicio
siguen abiertos en local, así que los ejemplos de más abajo funcionan por cualquiera de las dos vías.

El simulador de pagos no tiene API de negocio: solo responde `GET /health`. Todo lo que hace entra y
sale por el broker, y es deliberado — un endpoint técnico para forzar fallos habría convertido un
proveedor de pagos en una consola de pruebas.

Enrollment, Learning y Certification necesitan saber dónde está Course Authoring. Lo leen de
`Services:CourseAuthoring:BaseUrl`, que en Development apunta a `http://localhost:5195`, y **ninguno
de los tres arranca si falta**. La llamada se ajusta sin recompilar:

| Ajuste | Por defecto | Para qué |
|---|---|---|
| `Services:CourseAuthoring:BaseUrl` | `http://localhost:5195` | destino de la consulta al catálogo |
| `Services:CourseAuthoring:TotalTimeoutSeconds` | `3` (Certification, `5`) | presupuesto de la operación completa, reintentos y esperas incluidos |
| `Services:CourseAuthoring:RetryAttempts` | `2` | reintentos, no intentos: 2 reintentos son 3 intentos como mucho |
| `Services:CourseAuthoring:RetryBaseDelayMilliseconds` | `200` | base del backoff exponencial sin jitter: 200 ms y 400 ms |
| `Services:CourseAuthoring:CircuitBreakerFailureRatio` | `0.5` | proporción de fallos que abre el circuito |
| `Services:CourseAuthoring:CircuitBreakerSamplingSeconds` | `30` | ventana en la que se mide esa proporción |
| `Services:CourseAuthoring:CircuitBreakerMinimumThroughput` | `3` | resultados mínimos en la ventana antes de poder abrir |
| `Services:CourseAuthoring:CircuitBreakerBreakSeconds` | `15` | cuánto permanece abierto antes de probar en *half-open* |
| `Services:CourseAuthoring:RetryAfterSeconds` | `5` | valor de la cabecera `Retry-After` del `503` |

`TotalTimeoutSeconds` sustituye al antiguo `TimeoutSeconds`, que ya no se usa: el límite temporal es
ahora del pipeline entero, no de cada intento, y `HttpClient.Timeout` queda en infinito para que no
haya dos relojes compitiendo.

Enrollment, Learning y Certification también necesitan saber dónde está el broker. Lo leen de
`Messaging:RabbitMq`, que en Development apunta a `localhost:5672` con el usuario `lms`, y
**ninguno de los tres arranca si falta el host**. Fuera de Development, la contraseña se toma del
entorno (`Messaging__RabbitMq__Password`); nunca está en el código.

| Ajuste | Por defecto | Para qué |
|---|---|---|
| `Messaging:RabbitMq:Host` | `localhost` | dónde está el broker |
| `Messaging:RabbitMq:Port` | `5672` | puerto AMQP |
| `Messaging:Outbox:PollingIntervalSeconds` | `5` | cada cuánto revisan Enrollment y Learning si tienen mensajes por enviar |
| `Messaging:Outbox:BatchSize` | `20` | cuántos envía como mucho en cada vuelta |
| `Messaging:Outbox:PublishTimeoutSeconds` | `5` | cuánto espera como mucho cada envío |

Learning y Certification tienen además su propio trabajo de fondo:

| Ajuste | Por defecto | Para qué |
|---|---|---|
| `Projection:PollingIntervalSeconds` | `5` | cada cuánto actualiza Learning su modelo de lectura |
| `Projection:BatchSize` | `50` | cuántos cambios aplica como mucho en cada vuelta |
| `Certification:Issuer` | `LMS` | quién firma el certificado; se copia a la fila al emitirlo |
| `Certification:PollingIntervalSeconds` | `5` | cada cuánto revisa Certification si tiene certificados por emitir |
| `Certification:BatchSize` | `20` | cuántos intenta emitir como mucho en cada vuelta |

Certification necesita también el nombre visible del estudiante, que hoy se toma de
`Certification:StudentDirectory:Students`, un mapa de identificador a nombre. En Development trae
sembrado el estudiante del recorrido de más abajo. Sin entrada para un estudiante, su certificado no
se emite: nunca se inventa un nombre.

### 4. Documentación y estado

| Recurso | Course Authoring | Enrollment | Learning | Certification | BFF | Paid Enrollment | Disponible en |
|---|---|---|---|---|---|---|---|
| Documentación interactiva (Scalar) | `:5195/scalar/v1` | `:5196/scalar/v1` | `:5197/scalar/v1` | `:5198/scalar/v1` | `:5199/scalar/v1` | `:5200/scalar/v1` | solo Development |
| Documento OpenAPI | `:5195/openapi/v1.json` | `:5196/openapi/v1.json` | `:5197/openapi/v1.json` | `:5198/openapi/v1.json` | `:5199/openapi/v1.json` | `:5200/openapi/v1.json` | solo Development |
| Estado del servicio | `:5195/health` | `:5196/health` | `:5197/health` | `:5198/health` | `:5199/health` | `:5200/health` | siempre |

El simulador de pagos solo tiene `:5201/health`: no expone documentación porque no expone API. El
gateway tiene el suyo en `:5202/health`, que comprueba que el proceso vive, no el estado de sus
destinos; su documentación no existe porque no tiene API propia.

```bash
curl http://localhost:5195/health          # Healthy
curl http://localhost:5196/health          # Healthy
curl http://localhost:5197/health          # Healthy
curl http://localhost:5198/health          # Healthy
curl http://localhost:5199/health          # Healthy
curl http://localhost:5200/health          # Healthy
curl http://localhost:5201/health          # Healthy
curl http://localhost:5202/health          # Healthy
```

`/health` comprueba únicamente la conectividad de cada servicio con su propia base, no el estado del
esquema: si te saltas las migraciones, responde `Healthy` y el fallo aparece en la primera escritura.
El `/health` de Enrollment, Learning y Certification tampoco cambia porque Course Authoring o
RabbitMQ estén caídos: la salud de una dependencia no es la salud propia. El del BFF no tiene nada
que comprobar —no tiene base ni broker— y responde `Healthy` incluso con sus dos fuentes apagadas.

### 5. Obtener un token

Todo lo que no sea el catálogo, el contenido publicado de un curso o la verificación de un
certificado exige un token. Se obtiene de Keycloak con el flujo de código de autorización y PKCE,
contra el cliente público `lms-spa`, que es lo que hace la colección de Postman: al pulsar **Get New
Access Token** abre el formulario de Keycloak, se entra con uno de los cuatro usuarios de
demostración y el token queda guardado para las peticiones siguientes.

| Parámetro | Valor |
|---|---|
| Auth URL | `http://localhost:8080/realms/lms/protocol/openid-connect/auth` |
| Access Token URL | `http://localhost:8080/realms/lms/protocol/openid-connect/token` |
| Client ID | `lms-spa` |
| Code Challenge Method | `S256` |
| Callback URL | `https://oauth.pstmn.io/v1/callback` |

El token dura quince minutos y lleva las seis audiencias de las API. Cada servicio valida **solo la
suya**, además de la firma, el emisor y la expiración, así que un token no deja de comprobarse por
haber pasado ya por el gateway.

Para los ejemplos de más abajo, guarda el token en una variable:

```bash
TOKEN_INSTRUCTOR="<token del usuario instructor>"
TOKEN_ESTUDIANTE="<token del usuario estudiante1>"
TOKEN_ESTUDIANTE2="<token del usuario estudiante2>"
TOKEN_ADMIN="<token del usuario administrador>"
```

El actor de cada petición sale **siempre** del claim `sub` del token. No hay forma de decir «actúo
como este otro estudiante»: el cuerpo, la ruta y las cabeceras no pueden cambiarlo, y el gateway
elimina de toda petición entrante las cabeceras de identidad que existían antes.

### 6. Detener el entorno

```bash
docker compose down       # conserva los datos
docker compose down -v    # elimina también el volumen
```

## Uso

Los endpoints de negocio de ambos servicios están bajo `/api/v1/`. Los errores se devuelven siempre
como `application/problem+json`.

### Course Authoring

Los endpoints de autoría exigen un token con el rol `Instructor`; los de catálogo son públicos y no
piden nada.

| Método y ruta | Qué hace |
|---|---|
| `POST /api/v1/courses` | crea un curso en `Draft` |
| `GET /api/v1/courses` · `GET /api/v1/courses/{id}` | listado y detalle del instructor |
| `PATCH /api/v1/courses/{id}` | renombra el curso |
| `POST /api/v1/courses/{id}/lessons` | añade una lección al final |
| `PUT`/`DELETE /api/v1/courses/{id}/lessons/{lessonId}` | edita o elimina una lección |
| `PUT /api/v1/courses/{id}/lessons/order` | reordena por lote con la lista completa |
| `POST /api/v1/courses/{id}/publish` · `/republish` | publica o actualiza el contenido publicado |
| `GET /api/v1/catalog/courses?page=1&pageSize=20` · `/{id}` | catálogo público paginado y detalle |
| `GET /api/v1/catalog/courses/{courseId}/lesson-ids` | identificadores de las lecciones publicadas, en orden |

### Enrollment

Las tres rutas exigen un token con el rol `Student`. El estudiante sale del claim `sub`: el cuerpo
de la petición solo lleva `courseId`.

| Método y ruta | Qué hace |
|---|---|
| `POST /api/v1/enrollments` | matricula al estudiante en un curso publicado |
| `GET /api/v1/me/enrollments` | todas las matrículas del estudiante, sin paginar |
| `GET /api/v1/me/enrollments/{courseId}` | la matrícula del estudiante en ese curso |

Códigos de estado de `POST /api/v1/enrollments`:

| Código | Cuándo |
|---|---|
| `201` | la matrícula se ha creado en esta petición; incluye cabecera `Location` |
| `200` | ya existía una matrícula equivalente; devuelve la misma y no incluye `Location` |
| `400` | falta `courseId`, no es un GUID o es todo ceros |
| `422` | el curso no es matriculable: no existe o no está publicado |
| `503` | no se ha podido verificar si el curso es matriculable; incluye cabecera `Retry-After` |

Repetir la matrícula **no** es un error: el efecto de negocio ocurre una sola vez y la segunda
petición devuelve `200` con la misma matrícula. Un estudiante ya matriculado obtiene su `200` aunque
Course Authoring esté caído, porque en ese caso no hace falta preguntar nada.

Un curso inexistente y un curso sin publicar devuelven **el mismo** `422`: para el público, un curso
sin publicar no existe, y Enrollment no inventa una distinción que el catálogo no expone.

### Learning

Las cuatro rutas exigen un token con el rol `Student` y operan siempre sobre el progreso del
estudiante del token: no hay forma de leer ni de tocar el de otro.

El progreso lo crea Learning al recibir el mensaje de matrícula, y es su única forma de nacer:
ninguna de estas rutas lo crea. Mientras el mensaje no haya llegado, todas responden `404`.

| Método y ruta | Qué hace |
|---|---|
| `POST /api/v1/me/course-progress/{courseId}/completed-lessons` | marca una lección como completada |
| `POST /api/v1/me/course-progress/{courseId}/completion` | confirma la finalización si ya se cumple |
| `GET /api/v1/me/course-progress[?status=InProgress\|Completed]` | todos los progresos del estudiante |
| `GET /api/v1/me/course-progress/{courseId}` | el progreso del estudiante en ese curso |

Las cuatro devuelven la misma forma de respuesta, con el estado, la fecha de inicio, la de
finalización —`null` mientras el curso siga en curso—, los identificadores de las lecciones ya
completadas, cuántas son, cuántas tiene el curso y el porcentaje de avance:

```json
{ "studentId": "...", "courseId": "...", "status": "InProgress",
  "startedAt": "2026-08-14T10:00:00+00:00", "completedAt": null,
  "completedLessonIds": ["..."],
  "completedLessonCount": 1, "totalLessonCount": 3, "percentage": 33.33 }
```

`totalLessonCount` y `percentage` son `null` mientras nadie haya marcado todavía ninguna lección: en
ese momento no se sabe cuántas tiene el curso, y `0` afirmaría algo distinto y falso. Un curso
finalizado devuelve siempre `percentage: 100`.

Códigos de estado de las dos escrituras:

| Código | Cuándo |
|---|---|
| `200` | la escritura se ha aplicado, o no ha cambiado nada porque ya estaba hecha |
| `400` | `courseId` de la ruta todo ceros; `lessonId` falta, no es un GUID o es todo ceros; `status` distinto de los dos valores admitidos |
| `404` | el estudiante no tiene progreso en ese curso: al marcar una lección, al confirmar la finalización o al consultarlo |
| `422` | el curso no está publicado; la lección no pertenece al contenido publicado; confirmar sin haber completado todas las lecciones |
| `503` | no se ha podido saber cuáles son las lecciones publicadas; incluye cabecera `Retry-After` |

Los tres casos de `422` se distinguen por el `title` del `problem+json`, no por el código: en los
tres la petición está bien escrita y solo es inejecutable contra el estado actual del catálogo.

Marcar dos veces la misma lección **no** es un error, y confirmar una finalización ya sellada
tampoco: la segunda petición devuelve `200` con el mismo cuerpo y no escribe ninguna fila. La fecha
de finalización no se recalcula nunca; si el instructor publica lecciones nuevas después, se pueden
completar y el sello no cambia.

Un `courseId` que no es un GUID devuelve `404` en vez de `400`: la ruta exige un GUID, así que
ninguna ruta coincide y no hay nada que validar. Uno todo ceros sí coincide y se rechaza con `400`
sin llegar a preguntar a Course Authoring.

### Certification

Un certificado nace de una finalización sellada y no se puede pedir: aparece solo cuando Learning
comunica el hecho y Certification consigue resolver el nombre del estudiante y el título del curso.

| Método y ruta | Acceso | Qué hace |
|---|---|---|
| `GET /api/v1/certificates/{certificateId}` | **público, sin token** | verifica un certificado |
| `GET /api/v1/me/certificates` | propietario | los certificados del estudiante |
| `GET /api/v1/me/certificates/{certificateId}` | propietario | el detalle de uno propio |

La verificación pública devuelve solo lo mínimo para comprobar el logro ante un tercero: quién lo
obtuvo, de qué curso, cuándo lo terminó y quién lo firma.

```json
{ "certificateId": "...", "valid": true,
  "studentName": "Ada Lovelace", "courseTitle": "Microservicios con .NET 10",
  "completedAt": "2026-08-17T07:31:55+00:00", "issuer": "LMS" }
```

No incluye el identificador del estudiante ni el del curso ni la fecha de emisión: quien verifica no
necesita nada de eso. Un `certificateId` que no existe devuelve `404`, no un `200` con `valid: false`:
sobre un identificador inexistente no se puede afirmar nada.

Las dos rutas del propietario exigen un token con el rol `Student` y devuelven la vista completa,
con `studentId`, `courseId` e `issuedAt`. Pedir el detalle de un certificado de otro estudiante
devuelve `404`, no `403`: no se confirma siquiera que exista.

`studentName` y `courseTitle` quedan congelados en el momento de emitir. Si el instructor renombra el
curso después, el certificado sigue diciendo lo que decía el día que se emitió. `completedAt` es la
fecha de la finalización, no la de emisión: son distintas y ambas aparecen en la vista del
propietario.

Flujo completo — publicar un curso, matricularse, avanzar hasta finalizarlo y obtener el certificado:

```bash
# Tokens obtenidos en el paso 5. El identificador del actor sale del claim sub
TOKEN_INSTRUCTOR="<token del usuario instructor>"
TOKEN_ESTUDIANTE="<token del usuario estudiante1>"

# 1. Crear el curso en Course Authoring. Devuelve 201 con su id
curl -i -X POST http://localhost:5195/api/v1/courses \
  -H "Authorization: Bearer $TOKEN_INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10"}'

COURSE="<id devuelto>"

# 2. Agregar dos lecciones y publicar. Publicar exige al menos una leccion
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "Authorization: Bearer $TOKEN_INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Introduccion","description":"Que es un microservicio","videoUrl":"https://videos.example.com/1.mp4"}'

curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "Authorization: Bearer $TOKEN_INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Contextos","description":"Como se reparte el dominio","videoUrl":"https://videos.example.com/2.mp4"}'

curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/publish \
  -H "Authorization: Bearer $TOKEN_INSTRUCTOR"

# 3. Matricularse. Devuelve 201 y Location: /api/v1/me/enrollments/$COURSE
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"

# 4. Repetir la misma peticion. Devuelve 200, el mismo id y sin cabecera Location
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"

# 5. Consultar las matriculas propias
curl -s http://localhost:5196/api/v1/me/enrollments -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
curl -s http://localhost:5196/api/v1/me/enrollments/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"

# 5b. Consultar el progreso enseguida: todavia 404, el mensaje esta en camino
curl -i http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# HTTP/1.1 404 Not Found

# 5c. Repetir hasta que aparezca. Suele tardar unos segundos
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"studentId":"...","courseId":"...","status":"InProgress","startedAt":"...","completedAt":null,
#  "completedLessonIds":[]}

# 6. Pedir los identificadores de las lecciones publicadas del curso
curl -s http://localhost:5195/api/v1/catalog/courses/$COURSE/lesson-ids

LESSON1="<primer id devuelto>"
LESSON2="<segundo id devuelto>"

# 7. Marcar la primera leccion. Devuelve 200 con status "InProgress" y completedAt null
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"

# 8. Marcar la segunda. Al completar el contenido publicado, la finalizacion se sella aqui mismo:
#    status "Completed" y completedAt con fecha. No hace falta llamar a /completion
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON2\"}"

# 9. Repetir el marcado. Devuelve 200 con el mismo cuerpo, el mismo completedAt y sin escribir nada
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON2\"}"

# 10. Consultar el progreso propio, entero y filtrado por estado.
#     Devuelven percentage 100 y la lista de lecciones completadas
curl -s http://localhost:5197/api/v1/me/course-progress -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
curl -s "http://localhost:5197/api/v1/me/course-progress?status=Completed" -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"

# 11. Esperar unos segundos y pedir los certificados propios. Al principio la lista
#     esta vacia: el certificado tarda un momento en emitirse
curl -s http://localhost:5198/api/v1/me/certificates -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# [{"certificateId":"...","courseId":"...","courseTitle":"Microservicios con .NET 10",
#   "completedAt":"...","issuedAt":"..."}]

CERT="<certificateId devuelto>"

# 12. Verificarlo sin ninguna cabecera, como lo haria un tercero
curl -s http://localhost:5198/api/v1/certificates/$CERT
# {"certificateId":"...","valid":true,"studentName":"...","courseTitle":"...",
#  "completedAt":"...","issuer":"LMS"}

# 13. Ver el detalle propio, con studentId, courseId e issuedAt
curl -s http://localhost:5198/api/v1/me/certificates/$CERT -H "Authorization: Bearer $TOKEN_ESTUDIANTE"

# 14. Pedir ese mismo certificado como otro estudiante. Devuelve 404, no 403
curl -i http://localhost:5198/api/v1/me/certificates/$CERT \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE2"
```

Caso degradado — con Course Authoring apagado, ni se concede una matrícula nueva ni se registra
ninguna lección:

```bash
# Detener el proceso de Course Authoring (Ctrl+C en su terminal) y matricularse en otro curso
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d '{"courseId":"44444444-4444-4444-4444-444444444444"}'

# HTTP/1.1 503 Service Unavailable
# Retry-After: 5

# Marcar una leccion del curso publicado antes, con Course Authoring todavia apagado
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"

# HTTP/1.1 503 Service Unavailable
# Retry-After: 5
```

No se crea ni se modifica ninguna fila en ninguno de los dos casos. Enrollment prefiere no matricular
a matricular sin haber comprobado que el curso está publicado, y Learning prefiere no registrar nada
a registrar contra un contenido que no ha podido consultar: cualquier escritura puede ser la que
selle la finalización, y un sello contra información obsoleta no se puede deshacer.

### El progreso tarda un momento en aparecer

Entre el `201` de la matrícula y la aparición del progreso pasa un rato corto y variable. Durante esa
ventana es **correcto** que Learning responda `404` en las tres rutas que necesitan un progreso
concreto, incluido marcar una lección:

```bash
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"
# HTTP/1.1 201 Created

curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"
# HTTP/1.1 404 Not Found     <- el mensaje aun no ha llegado

# Esperar unos segundos y repetir la misma peticion
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"
# HTTP/1.1 200 OK
```

No hay que hacer nada para que se cierre la ventana: se cierra sola. No se promete cuánto dura.

### Las dos consultas de progreso van un momento por detrás

Las dos rutas `GET` de Learning no leen el mismo sitio donde se escribe: leen una vista que se
actualiza a partir de las escrituras, unos segundos después. Por eso puede pasar esto:

```bash
# Marcar una leccion. El POST responde ya con el estado nuevo
curl -s -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"
# {"status":"InProgress","completedLessonCount":1,"totalLessonCount":3,"percentage":33.33, ...}

# Consultarlo enseguida: todavia puede devolver el estado anterior
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"completedLessonCount":0,"totalLessonCount":null,"percentage":null, ...}

# Repetir unos segundos despues: ya coincide
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"completedLessonCount":1,"totalLessonCount":3,"percentage":33.33, ...}
```

No es un fallo y no hay nada que reintentar: la respuesta del `POST` siempre viene del dato recién
escrito, y las consultas se ponen al día solas. Un progreso que acaba de nacer tampoco aparece de
inmediato en el `GET`, por el mismo motivo.

### Un curso finalizado muestra 100 % aunque después crezca

Si el instructor añade lecciones a un curso ya publicado y lo republica, un estudiante que ya lo
había finalizado puede completar las nuevas. El recuento sube y el total también, pero la
finalización no se toca y el porcentaje se queda en `100`:

```bash
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"status":"Completed","completedAt":"2026-08-17T07:31:55+00:00",
#  "completedLessonCount":4,"totalLessonCount":5,"percentage":100}
```

`4` de `5` no es `80 %` aquí: el estudiante terminó el curso que existía cuando lo terminó, y el
contenido añadido después no le quita el logro.

### El certificado puede tardar, y a veces espera a propósito

Certification acepta la finalización en cuanto llega, pero para emitir necesita dos datos que no
viajan en el mensaje: el título del curso, que pide a Course Authoring, y el nombre visible del
estudiante. Mientras falte cualquiera de los dos, el trabajo queda anotado y se reintenta solo.

```bash
# Con Course Authoring apagado, finalizar un curso y pedir los certificados propios
curl -s http://localhost:5198/api/v1/me/certificates -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# []      <- el trabajo esta anotado, pero todavia no hay nada que emitir

# Arrancar Course Authoring y esperar unos segundos
curl -s http://localhost:5198/api/v1/me/certificates -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# [{"certificateId":"...","courseTitle":"Microservicios con .NET 10", ...}]
```

No hace falta repetir la finalización ni tocar nada: el certificado aparece solo cuando las dos
fuentes vuelven. Lo que **no** ocurre nunca es que se emita a medias, con el título vacío o con un
nombre inventado; antes de eso, espera indefinidamente.

Un estudiante sin nombre configurado se comporta igual: la finalización queda registrada y el
certificado espera hasta que ese nombre exista.

### Con RabbitMQ apagado

La matrícula se concede igual; lo único que se retrasa es el progreso.

```bash
docker compose stop rabbitmq

curl -i http://localhost:5196/health
# HTTP/1.1 200 OK    <- que el broker este caido no enferma al servicio

curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"
# HTTP/1.1 201 Created    <- la matricula existe

curl -i http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# HTTP/1.1 404 Not Found  <- y seguira asi mientras el broker no vuelva
```

El mensaje queda guardado en la base de Enrollment, esperando:

```bash
docker exec -e PGPASSWORD=enrollment_dev lms-postgres \
  psql -U enrollment_user -d enrollment \
  -c "SELECT id, published_at, attempt_count FROM outbox_messages WHERE published_at IS NULL"
```

Al volver a arrancar el broker, el progreso aparece solo, sin repetir ninguna petición:

```bash
docker compose start rabbitmq

# Esperar a que el contenedor este healthy y consultar de nuevo
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"status":"InProgress","startedAt":"...", ...}
```

El `startedAt` que aparece es el instante en que se concedió la matrícula, no aquel en que el broker
se recuperó.

### Matricularse sin haber arrancado Learning

Learning puede estar apagado —incluso no haberse arrancado nunca— cuando alguien se matricula. El
mensaje espera en la cola:

```bash
# Detener Learning (Ctrl+C en su terminal) y matricularse en otro curso publicado
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$OTRO_COURSE\"}"
# HTTP/1.1 201 Created
```

En <http://localhost:15672>, la cola `lms.learning.student-enrolled` muestra un mensaje pendiente.
Al arrancar Learning otra vez, lo consume y el progreso aparece:

```bash
dotnet run --project src/services/learning/Learning.Api --launch-profile http

curl -s http://localhost:5197/api/v1/me/course-progress/$OTRO_COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
```

> **Todo viaja sin cifrar y el realm es de desarrollo.** Keycloak corre en HTTP, con `sslRequired`
> desactivado y contraseñas de demostración versionadas en `deploy/keycloak/realm-lms.json`. Los
> tokens son válidos, pero el transporte no protege nada: no expongas estos servicios fuera de tu
> máquina.

> **Las llamadas a Course Authoring llevan timeout, reintento y cortacircuitos.** Enrollment,
> Learning y Certification aplican un pipeline explícito —timeout total, hasta dos reintentos con
> backoff exponencial de 200 ms y 400 ms sin jitter, y un Circuit Breaker— sobre cada consulta al
> catálogo. Solo se reintentan los fallos transitorios: transporte caído, `408` y `500`–`599`. Un
> `404` es una respuesta funcional y no se reintenta nunca. Si la operación completa no cabe en
> `TotalTimeoutSeconds`, o si el circuito está abierto, la petición se resuelve con `503` y
> `Retry-After`, igual que antes: la resiliencia no cambia ningún código de estado de la v1.

> Los cuerpos de ejemplo van sin acentos a propósito: pegar caracteres no ASCII en una terminal cuya
> página de códigos no es UTF-8 —el caso por defecto en Windows— los envía mal codificados. Si
> necesitas acentos, manda el cuerpo desde un archivo: `--data-binary @leccion.json`.

## Compra de acceso a un curso

Necesita todas las unidades arrancadas. El estudiante pide comprar el acceso, y a partir de ahí el
proceso avanza solo: Paid Enrollment comprueba que no tenga ya acceso, autoriza el pago, lo captura,
pide a Enrollment la matrícula y confirma la compra.

### Poner precio a un curso

Un curso sin precio configurado **no se puede comprar**. No hay precio por defecto: uno lo habría
convertido en un cobro por un importe que nadie decidió.

Los precios se declaran en `src/services/paid-enrollment/PaidEnrollment.Api/appsettings.Development.json`,
con el `CourseId` real del curso que hayas creado:

```json
"Purchase": {
  "AmountsByCourse": {
    "01a0159f-e147-7953-b326-ed8a6b2fb8ed": "50.00"
  }
}
```

Hay que reiniciar Paid Enrollment para que lea la tabla. Un curso sin entrada responde `422` ·
`PurchaseAmountNotConfigured`.

### El camino feliz

```bash
STUDENT=44444444-4444-4444-4444-444444444444
COURSE=<id-de-un-curso-publicado-con-precio>

curl -i -X POST http://localhost:5200/api/v1/purchases \
  -H "Authorization: Bearer $TOKEN_ESTUDIANTE" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"
```

Responde **`202 Accepted`** con `Location` y `status: "Started"`. El `202` no significa que el pago
se haya realizado ni que la matrícula exista: significa que la compra se aceptó para procesar, y nada
más.

El estado se consulta por su identificador, tantas veces como haga falta:

```bash
curl -s http://localhost:5200/api/v1/purchases/<purchaseId> -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
```

La compra atraviesa `CheckingAccess`, `AuthorizingPayment`, `PaymentAuthorized`, `CapturingPayment`,
`PaymentCaptured`, `GrantingEnrollment` y `EnrollmentGranted` hasta llegar a `Confirmed`. Los
diecisiete estados son consultables a propósito: una Saga cuyos estados no se ven desde fuera no hay
forma de diagnosticarla cuando se queda a medias.

Unos segundos después, el resto del sistema se ha enterado:

```bash
curl -s http://localhost:5196/api/v1/me/enrollments/$COURSE   -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
curl -s http://localhost:5199/api/v1/me/courses-in-progress    -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
```

La matrícula aparece con `"type": "Paid"`, y el progreso lo creó el mismo mensaje de siempre:
`StudentEnrolled`. Learning no sabe que hubo una compra, y no le hace falta.

### Provocar cada desenlace

El simulador de pagos se comporta según **el importe**, no según ninguna bandera en el mensaje. No
existe `forceFailure` ni endpoint de simulación: un contrato de negocio que llevara instrucciones de
prueba dejaría de ser un contrato de negocio.

| Importe | Qué hace | Cómo acaba la compra |
|---|---|---|
| cualquiera sin regla | éxito completo | `Confirmed` |
| `13.00` | rechaza la autorización | `Rejected(PaymentDeclined)` |
| `17.00` | autoriza y **pierde la respuesta** | reconcilia y sigue |
| `19.00` | falla la captura | `Compensated(AuthorizationVoided)` |
| `23.00` | captura y **pierde la respuesta** | reconcilia y sigue |
| `29.00` | falla el reembolso | `ManualReview(RefundFailed)` |
| `31.00` | reembolsa y **pierde la respuesta** | reconcilia y sigue |

Las reglas `Silent*` no fingen nada: **aplican el efecto y lo persisten**, y lo único que se pierde
es la respuesta. Un resultado desconocido de verdad es un reply perdido; responder «no sé» sería un
resultado, no un desconocido.

**Compensación por reembolso.** Pon precio a un curso que esté **en Borrador** y cómpralo. El pago se
autoriza y se captura, Enrollment rechaza la concesión porque el curso no está publicado, y la compra
termina en `Compensated(PaymentRefunded)`. En `payments` quedan `captured_at` y `refunded_at`, y en
`enrollments` no habrá ninguna fila nueva.

**Compensación por anulación.** Con el importe `19.00`, la captura falla y no llega a cobrar. La
compensación entonces es una anulación, no un reembolso: `Compensated(AuthorizationVoided)`, y
`captured_at` queda nulo.

**Reconciliación.** Con el importe `23.00`, la captura ocurre pero su respuesta se pierde. Tras unos
segundos la compra entra en `VerifyingCaptureOutcome` y **pregunta** el estado del pago en vez de
reenviar la captura — un comando de cobro reenviado sería indistinguible de una orden de cobrar. La
respuesta trae el instante real del cobro, y con él la compra continúa hasta `Confirmed`.

Se ve bien en la base:

```sql
-- en la base purchase
SELECT message_type, count(*) FROM outbox_messages
WHERE aggregate_id = '<purchaseId>' GROUP BY message_type;
-- CapturePayment    1     <- nunca se cobra dos veces
-- GetPaymentStatus  1..3  <- una por intento de reconciliación
```

**Resultado desconocido de verdad.** Con el importe `23.00`, apaga el simulador en cuanto la compra
entre en `CapturingPayment`. Agotados los intentos, queda en `ManualReview(CaptureOutcomeUnknown)`
**sin reembolsar nada**: no se compensa lo que no se ha podido comprobar.

### Resolver una compra suspendida

`ManualReview` no es un estado terminal ni un error: es el sistema diciendo que no puede resolver el
caso solo con seguridad. Se sale por una de cuatro resoluciones, y todas exigen evidencia y un
operador:

```bash
curl -i -X POST http://localhost:5200/api/v1/purchases/<purchaseId>/resolutions \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -H "Content-Type: application/json" \
  -d '{"resolution":"CloseWithoutAutomaticAction","evidence":"El proveedor no responde"}'
```

| Resolución | Cuándo es válida |
|---|---|
| `ResolveAsConfirmed` | consta la captura, no consta anulación ni reembolso, y la matrícula la concedió esta misma compra |
| `RetryCompensation` | consta una autorización que compensar |
| `ResolveAsCompensated` | consta una anulación o un reembolso |
| `CloseWithoutAutomaticAction` | siempre |

Una resolución cuya precondición no se cumple responde `422` diciendo cuál falla. Sobre una compra
que ya no está en `ManualReview`, `409`.

**Una compra cerrada bloquea el par estudiante–curso.** `CloseWithoutAutomaticAction` cierra un caso
cuyo resultado de pago **quedó sin establecer**, y eso no demuestra que no se cobrara. Abrir otra
compra autorizaría un segundo pago sobre un posible cargo anterior, así que `POST /purchases` de ese
mismo par responde `409` · `PurchaseClosedForCourse`. Es el lado seguro del compromiso; desbloquearlo
exige hoy intervenir sobre la base.

### Lo que no se puede duplicar

Reencolar mensajes a mano desde <http://localhost:15672> no rompe nada, y merece la pena comprobarlo:

- el mismo mensaje otra vez lo corta el Inbox por `MessageId`;
- un reintento con `MessageId` nuevo lo corta la clave de negocio — `PaymentId` en el proveedor,
  `PurchaseId` en el ledger de Enrollment;
- una respuesta que ya no corresponde al estado actual no transiciona nada;
- y una respuesta correlacionada con **otra** compra no se aplica ni se deduplica: se aparta íntegra
  en la cola `..._error` con el log `saga-correlation-mismatch`, porque un mensaje bien formado y mal
  correlacionado pasaría los tres filtros anteriores y corrompería el estado.

Reenviar la concesión con el mismo `PurchaseId` devuelve el resultado almacenado, **no crea una
segunda matrícula** y **no vuelve a publicar** `StudentEnrolled`.

## BFF de composición

`bff-composition` no es un microservicio de negocio: no tiene dominio, ni base de datos, ni
migraciones. Es una unidad **técnica** que compone en una sola respuesta lo que hoy exige dos
llamadas —el progreso del estudiante en Learning y el catálogo de Course Authoring—, y que declara
por escrito qué parte de esa respuesta falta cuando una de las dos fuentes no está.

```bash
dotnet run --project src/bff/BffComposition.Api --launch-profile http
```

Escucha en `http://localhost:5199`. Lee `Services:Learning:BaseUrl` y
`Services:CourseAuthoring:BaseUrl`, y **no arranca si falta cualquiera de las dos**. Lo que sí hace
es arrancar con Learning y Course Authoring apagados: comprueba configuración, no conectividad.

| Ajuste | Por defecto | Para qué |
|---|---|---|
| `Services:Learning:BaseUrl` | `http://localhost:5197` | dependencia **esencial** |
| `Services:Learning:TotalTimeoutSeconds` | `5` | presupuesto de la llamada completa, reintentos incluidos |
| `Services:Learning:RetryAfterSeconds` | `5` | valor de la cabecera `Retry-After` del `503` |
| `Services:CourseAuthoring:BaseUrl` | `http://localhost:5195` | dependencia de **enriquecimiento** |
| `Services:CourseAuthoring:TotalTimeoutSeconds` | `4` | menor que el de Learning: un catálogo lento no agota el presupuesto de la petición |
| `Services:CourseAuthoring:MaxEnrichmentConcurrency` | `8` | cuántos cursos se enriquecen a la vez |

Las dos dependencias tienen además `RetryAttempts`, `RetryBaseDelayMilliseconds` y los cuatro
parámetros `CircuitBreaker*`, con la misma forma que en los tres servicios.

### La ruta

```
GET /api/v1/me/courses-in-progress
GET /api/v1/me/courses-in-progress?status=InProgress
GET /api/v1/me/courses-in-progress?status=Completed
```

Exige un token con el rol `Student`, que el BFF valida y propaga hacia Learning. Sin `status`
devuelve los cursos en progreso. El filtro no
distingue mayúsculas —`?status=completed` es lo mismo que `?status=Completed`—, igual que Learning;
cualquier otro valor, incluido el vacío, responde `400` sin llegar a llamar a Learning.

Continuando el recorrido de más arriba, con los cinco procesos arriba:

```bash
curl -s http://localhost:5199/api/v1/me/courses-in-progress -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"items":[{"courseId":"...","courseTitle":"Microservicios con .NET 10","lessonCount":2,
#            "status":"InProgress","startedAt":"...","completedAt":null,
#            "completedLessonCount":0,"percentage":null}],
#  "isPartial":false,"warnings":[]}
```

`courseTitle` y `lessonCount` los pone Course Authoring; el resto es de Learning. El BFF no calcula
ningún campo: `percentage` es el de Learning y `lessonCount` el del catálogo. Cada número tiene un
único dueño.

### Degradación parcial: apaga solo Course Authoring

```bash
# Deten el proceso de Course Authoring y repite la misma peticion
curl -s http://localhost:5199/api/v1/me/courses-in-progress -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# {"items":[{"courseId":"...","courseTitle":null,"lessonCount":null,
#            "status":"InProgress","startedAt":"...","completedAt":null,
#            "completedLessonCount":0,"percentage":null}],
#  "isPartial":true,
#  "warnings":[{"courseId":"...","code":"CourseEnrichmentUnavailable",
#               "message":"No se pudo obtener del catalogo la informacion del curso ..."}]}
```

Sigue siendo `200`: Learning respondió, así que la respuesta es verdadera aunque incompleta. Los
datos de progreso llegan íntegros, los dos campos del catálogo son `null` —nunca un título
inventado ni `lessonCount: 0`— e `isPartial` con `warnings[]` lo declaran. Al volver a levantar
Course Authoring, la petición siguiente vuelve a `isPartial: false` sin reiniciar el BFF.

### Dependencia esencial: apaga solo Learning

```bash
curl -i -s http://localhost:5199/api/v1/me/courses-in-progress -H "Authorization: Bearer $TOKEN_ESTUDIANTE"
# HTTP/1.1 503 Service Unavailable
# Retry-After: 5
# Content-Type: application/problem+json
# {"title":"Dependencia esencial no disponible","status":503,
#  "detail":"No se pudo obtener el progreso del estudiante desde Learning."}
```

Aquí no hay degradación posible. Solo Learning sabe en qué cursos está matriculado el estudiante:
una lista construida con el catálogo sería una lista de cursos ajenos presentada como si fuera su
progreso. El BFF prefiere no responder a responder mal.

### Qué tardan las cosas

Medido en este repositorio, con `curl -o /dev/null -s -w "%{time_total}\n"`:

| Situación | Tiempo | Respuesta |
|---|---|---|
| Todo arriba | ≈ 0,02–0,4 s | `200` completo |
| Course Authoring caído | ≈ 4,0 s | `200` degradado |
| Learning caído | ≈ 5,0 s | `503` + `Retry-After: 5` |
| `POST /api/v1/enrollments` con el catálogo caído | ≈ 3,0 s | `503` + `Retry-After: 5` |
| Matrícula en un curso inexistente, catálogo arriba | ≈ 0,1 s | `422`, sin reintentos |

Cada dependencia termina dentro de su `TotalTimeoutSeconds` y ninguna petición queda colgada. El
contraste entre los ≈ 0,1 s del `422` y los ≈ 3,0 s de la indisponibilidad separa el fallo funcional,
que no se reintenta, del transitorio, que sí.

> **El Circuit Breaker está configurado pero no llega a abrirse deteniendo un proceso.** En Windows,
> rechazar una conexión contra un puerto muerto tarda ≈ 2 s, así que el timeout total cancela la
> operación antes de que se acumulen los tres fallos que el breaker necesita, y una cancelación no
> se contabiliza como fallo. La configuración está puesta y el orden del pipeline es el correcto;
> verlo abrirse exige una dependencia que falle rápido, y eso sigue pendiente.

> **El `/health` del BFF no consulta a Learning ni a Course Authoring.** Responde `Healthy` con las
> dos apagadas, y es deliberado: un componente cuyo valor es responder degradado no debe declararse
> enfermo por una dependencia caída, o un orquestador lo reiniciaría justo cuando más útil resulta.
> La disponibilidad de las fuentes se observa en `isPartial`, en `warnings[]` y en el `503`.

## Pruebas

```bash
dotnet test LMS.sln
```

| Proyecto | Qué prueba | Necesita Docker |
|---|---|---|
| `CourseAuthoring.Domain.Tests` | invariantes del agregado: propiedad, posiciones, publicación y no-op | no |
| `CourseAuthoring.Application.Tests` | orquestación y autorización con dobles de los puertos | no |
| `CourseAuthoring.Integration.Tests` | repositorio, consultas del catálogo y la API completa | sí |
| `Enrollments.Domain.Tests` | invariantes de la matrícula: identidades y tipo gratuito | no |
| `Enrollments.Application.Tests` | orquestación, idempotencia y curso no verificable, con dobles | no |
| `Enrollments.Integration.Tests` | repositorio, índice único, API completa, cliente HTTP del catálogo y aislamiento entre bases | sí |
| `Learning.Domain.Tests` | invariantes del progreso: lección actual, idempotencia y sello irreversible | no |
| `Learning.Application.Tests` | orquestación, no-op sin escritura y reintento tras perder una carrera, con dobles | no |
| `Learning.Integration.Tests` | repositorio, claves primarias compuestas, API completa, cliente HTTP del contenido publicado, colisión real de concurrencia y aislamiento entre las tres bases | sí |

Las pruebas de integración levantan sus propios contenedores PostgreSQL con Testcontainers, les
aplican las migraciones y no tocan la base del `docker-compose`. Las de aislamiento arrancan además
un contenedor con los scripts reales de `deploy/postgres/init/` y comprueban que ninguno de los tres
usuarios de servicio puede conectarse a las bases de los otros dos.

La colisión de concurrencia se provoca de verdad: una conexión independiente escribe la fila
ganadora entre la lectura y el guardado de la petición, y la prueba comprueba que la petición
termina en `200` con el mismo número de filas que si no se hubiera repetido.
