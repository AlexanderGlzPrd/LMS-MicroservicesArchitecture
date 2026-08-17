# LMS — Plataforma de aprendizaje basada en microservicios

Plataforma de formación donde un instructor crea cursos con lecciones, los publica y un público
general los consulta en un catálogo; un estudiante puede matricularse gratuitamente en los cursos
publicados y avanzar por ellos hasta finalizarlos. El objetivo es construir, de forma incremental,
una plataforma de aprendizaje sobre una arquitectura de microservicios en .NET.

Hoy el repositorio contiene cuatro microservicios ejecutables y un BFF de composición:

| Unidad | Puerto | Qué hace |
|---|---|---|
| **Course Authoring** | `5195` | autoría de cursos, publicación y catálogo público |
| **Enrollment** | `5196` | matrícula gratuita de un estudiante en un curso publicado |
| **Learning** | `5197` | lecciones completadas de un estudiante y finalización del curso |
| **Certification** | `5198` | emisión del certificado y su verificación pública |
| **BFF de composición** | `5199` | una sola respuesta con el progreso del estudiante y los datos del catálogo |

Cada uno de los cuatro servicios tiene su propia base de datos y su propio usuario de PostgreSQL,
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

## Tecnologías

- .NET 10 · ASP.NET Core (controladores)
- Entity Framework Core 10 · Npgsql · PostgreSQL 17
- Llamada síncrona entre servicios con `HttpClient` y timeout acotado
- RabbitMQ 4 y MassTransit para el mensaje entre Enrollment y Learning
- Versionado de API con `Asp.Versioning`
- OpenAPI + Scalar para documentación interactiva
- Docker Compose para la base de datos y el broker locales
- xUnit y Testcontainers para las pruebas

## Requisitos

| Requisito | Versión | Para qué |
|---|---|---|
| .NET SDK | 10.0.302 o superior dentro de 10.0 (fijada en `global.json`) | compilar y ejecutar |
| Docker Desktop | reciente | PostgreSQL y RabbitMQ locales, y pruebas de integración |
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

### 1. Levantar PostgreSQL y RabbitMQ

```bash
docker compose down -v     # obligatorio si ya tenías el volumen de una versión anterior
docker compose up -d
docker compose ps          # esperar STATUS = healthy en los dos contenedores
```

Levanta dos contenedores:

| Contenedor | Puertos | Qué es |
|---|---|---|
| `lms-postgres` | `5432` | PostgreSQL 17 |
| `lms-rabbitmq` | `5672` (AMQP) · `15672` (interfaz de gestión) | RabbitMQ 4 |

La interfaz de gestión del broker está en <http://localhost:15672> con usuario `lms` y contraseña
`lms`. Desde ahí se ven el exchange, la cola, cuántos mensajes hay pendientes y la cola de errores,
sin escribir una sola consulta.

Esas credenciales, el exchange `lms.enrollment`, la cola `lms.learning.student-enrolled` y sus
enlaces salen de `deploy/rabbitmq/definitions.json`, que el broker importa al arrancar. Es la única
fuente de su configuración inicial: el Compose no declara usuario ni contraseña por su cuenta. El
archivo documenta cómo regenerar el hash de la contraseña si quieres cambiarla.

PostgreSQL, en su primer arranque, ejecuta los scripts de `deploy/postgres/init/`, que crean:

| Base | Usuario de servicio | Contraseña local |
|---|---|---|
| `course_authoring` | `course_authoring_user` | `course_authoring_dev` |
| `enrollment` | `enrollment_user` | `enrollment_dev` |
| `learning` | `learning_user` | `learning_dev` |
| `certification` | `certification_user` | `certification_dev` |

Los cuatro scripts revocan el permiso de conexión que PostgreSQL concede por defecto a todos los roles,
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

### 2. Aplicar las migraciones de los cuatro servicios

Paso obligatorio: las API no aplican migraciones al arrancar.

```bash
dotnet ef database update --project src/services/course-authoring/CourseAuthoring.Infrastructure
dotnet ef database update --project src/services/enrollment/Enrollments.Infrastructure
dotnet ef database update --project src/services/learning/Learning.Infrastructure
dotnet ef database update --project src/services/certification/Certification.Infrastructure
```

Un comando por servicio, no por migración: Learning tiene varias y `database update` las aplica
todas en orden hasta dejar la base al día.

Cada fábrica de tiempo de diseño usa por defecto su cadena de conexión local. Para apuntar a otra
base, define la variable correspondiente:

```bash
# PowerShell
$env:COURSE_AUTHORING_CONNECTION = "Host=...;Database=...;Username=...;Password=..."
$env:ENROLLMENT_CONNECTION       = "Host=...;Database=...;Username=...;Password=..."
$env:LEARNING_CONNECTION         = "Host=...;Database=...;Username=...;Password=..."
$env:CERTIFICATION_CONNECTION    = "Host=...;Database=...;Username=...;Password=..."
```

### 3. Arrancar los cuatro servicios y el BFF

Cada uno en su propia terminal:

```bash
dotnet run --project src/services/course-authoring/CourseAuthoring.Api --launch-profile http
dotnet run --project src/services/enrollment/Enrollments.Api --launch-profile http
dotnet run --project src/services/learning/Learning.Api --launch-profile http
dotnet run --project src/services/certification/Certification.Api --launch-profile http
dotnet run --project src/bff/BffComposition.Api --launch-profile http
```

Escuchan en `http://localhost:5195`, `http://localhost:5196`, `http://localhost:5197`,
`http://localhost:5198` y `http://localhost:5199`, con `ASPNETCORE_ENVIRONMENT=Development`. El BFF
solo hace falta para la vista compuesta; los cuatro servicios funcionan sin él.

Enrollment, Learning y Certification necesitan saber dónde está Course Authoring. Lo leen de
`Services:CourseAuthoring:BaseUrl`, que en Development apunta a `http://localhost:5195`, y **ninguno
de los tres arranca si falta**. Puedes ajustar la llamada sin recompilar:

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

| Recurso | Course Authoring | Enrollment | Learning | Certification | BFF | Disponible en |
|---|---|---|---|---|---|---|
| Documentación interactiva (Scalar) | `:5195/scalar/v1` | `:5196/scalar/v1` | `:5197/scalar/v1` | `:5198/scalar/v1` | `:5199/scalar/v1` | solo Development |
| Documento OpenAPI | `:5195/openapi/v1.json` | `:5196/openapi/v1.json` | `:5197/openapi/v1.json` | `:5198/openapi/v1.json` | `:5199/openapi/v1.json` | solo Development |
| Estado del servicio | `:5195/health` | `:5196/health` | `:5197/health` | `:5198/health` | `:5199/health` | siempre |

```bash
curl http://localhost:5195/health          # Healthy
curl http://localhost:5196/health          # Healthy
curl http://localhost:5197/health          # Healthy
curl http://localhost:5198/health          # Healthy
curl http://localhost:5199/health          # Healthy
```

`/health` comprueba únicamente la conectividad de cada servicio con su propia base, no el estado del
esquema: si te saltas las migraciones, responde `Healthy` y el fallo aparece en la primera escritura.
El `/health` de Enrollment, Learning y Certification tampoco cambia porque Course Authoring o
RabbitMQ estén caídos: la salud de una dependencia no es la salud propia. El del BFF no tiene nada
que comprobar —no tiene base ni broker— y responde `Healthy` incluso con sus dos fuentes apagadas.

### 5. Detener el entorno

```bash
docker compose down       # conserva los datos
docker compose down -v    # elimina también el volumen
```

## Uso

Los endpoints de negocio de ambos servicios están bajo `/api/v1/`. Los errores se devuelven siempre
como `application/problem+json`.

### Course Authoring

Los endpoints de autoría exigen la cabecera `X-Instructor-Id` (un GUID); los de catálogo son
públicos.

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

Las tres rutas exigen la cabecera `X-Student-Id` (un GUID).

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
| `400` | falta `courseId`, no es un GUID o es todo ceros; o la cabecera `X-Student-Id` falta, no es un GUID o es todo ceros |
| `422` | el curso no es matriculable: no existe o no está publicado |
| `503` | no se ha podido verificar si el curso es matriculable; incluye cabecera `Retry-After` |

Repetir la matrícula **no** es un error: el efecto de negocio ocurre una sola vez y la segunda
petición devuelve `200` con la misma matrícula. Un estudiante ya matriculado obtiene su `200` aunque
Course Authoring esté caído, porque en ese caso no hace falta preguntar nada.

Un curso inexistente y un curso sin publicar devuelven **el mismo** `422`: para el público, un curso
sin publicar no existe, y Enrollment no inventa una distinción que el catálogo no expone.

### Learning

Las cuatro rutas exigen la cabecera `X-Student-Id` (un GUID) y operan siempre sobre el progreso de
ese estudiante: no hay forma de leer ni de tocar el de otro.

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
| `400` | `X-Student-Id` falta, no es un GUID o es todo ceros; `courseId` de la ruta todo ceros; `lessonId` falta, no es un GUID o es todo ceros; `status` distinto de los dos valores admitidos |
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
| `GET /api/v1/certificates/{certificateId}` | **público, sin cabecera** | verifica un certificado |
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

Las dos rutas del propietario exigen `X-Student-Id` y devuelven la vista completa, con `studentId`,
`courseId` e `issuedAt`. Pedir el detalle de un certificado de otro estudiante devuelve `404`, no
`403`: no se confirma siquiera que exista.

`studentName` y `courseTitle` quedan congelados en el momento de emitir. Si el instructor renombra el
curso después, el certificado sigue diciendo lo que decía el día que se emitió. `completedAt` es la
fecha de la finalización, no la de emisión: son distintas y ambas aparecen en la vista del
propietario.

Flujo completo — publicar un curso, matricularse, avanzar hasta finalizarlo y obtener el certificado:

```bash
INSTRUCTOR="11111111-1111-1111-1111-111111111111"
STUDENT="22222222-2222-2222-2222-222222222222"

# 1. Crear el curso en Course Authoring. Devuelve 201 con su id
curl -i -X POST http://localhost:5195/api/v1/courses \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10"}'

COURSE="<id devuelto>"

# 2. Agregar dos lecciones y publicar. Publicar exige al menos una leccion
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Introduccion","description":"Que es un microservicio","videoUrl":"https://videos.example.com/1.mp4"}'

curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Contextos","description":"Como se reparte el dominio","videoUrl":"https://videos.example.com/2.mp4"}'

curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/publish \
  -H "X-Instructor-Id: $INSTRUCTOR"

# 3. Matricularse. Devuelve 201 y Location: /api/v1/me/enrollments/$COURSE
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"

# 4. Repetir la misma peticion. Devuelve 200, el mismo id y sin cabecera Location
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"

# 5. Consultar las matriculas propias
curl -s http://localhost:5196/api/v1/me/enrollments -H "X-Student-Id: $STUDENT"
curl -s http://localhost:5196/api/v1/me/enrollments/$COURSE -H "X-Student-Id: $STUDENT"

# 5b. Consultar el progreso enseguida: todavia 404, el mensaje esta en camino
curl -i http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
# HTTP/1.1 404 Not Found

# 5c. Repetir hasta que aparezca. Suele tardar unos segundos
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
# {"studentId":"...","courseId":"...","status":"InProgress","startedAt":"...","completedAt":null,
#  "completedLessonIds":[]}

# 6. Pedir los identificadores de las lecciones publicadas del curso
curl -s http://localhost:5195/api/v1/catalog/courses/$COURSE/lesson-ids

LESSON1="<primer id devuelto>"
LESSON2="<segundo id devuelto>"

# 7. Marcar la primera leccion. Devuelve 200 con status "InProgress" y completedAt null
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"

# 8. Marcar la segunda. Al completar el contenido publicado, la finalizacion se sella aqui mismo:
#    status "Completed" y completedAt con fecha. No hace falta llamar a /completion
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON2\"}"

# 9. Repetir el marcado. Devuelve 200 con el mismo cuerpo, el mismo completedAt y sin escribir nada
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON2\"}"

# 10. Consultar el progreso propio, entero y filtrado por estado.
#     Devuelven percentage 100 y la lista de lecciones completadas
curl -s http://localhost:5197/api/v1/me/course-progress -H "X-Student-Id: $STUDENT"
curl -s "http://localhost:5197/api/v1/me/course-progress?status=Completed" -H "X-Student-Id: $STUDENT"
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"

# 11. Esperar unos segundos y pedir los certificados propios. Al principio la lista
#     esta vacia: el certificado tarda un momento en emitirse
curl -s http://localhost:5198/api/v1/me/certificates -H "X-Student-Id: $STUDENT"
# [{"certificateId":"...","courseId":"...","courseTitle":"Microservicios con .NET 10",
#   "completedAt":"...","issuedAt":"..."}]

CERT="<certificateId devuelto>"

# 12. Verificarlo sin ninguna cabecera, como lo haria un tercero
curl -s http://localhost:5198/api/v1/certificates/$CERT
# {"certificateId":"...","valid":true,"studentName":"...","courseTitle":"...",
#  "completedAt":"...","issuer":"LMS"}

# 13. Ver el detalle propio, con studentId, courseId e issuedAt
curl -s http://localhost:5198/api/v1/me/certificates/$CERT -H "X-Student-Id: $STUDENT"

# 14. Pedir ese mismo certificado como otro estudiante. Devuelve 404, no 403
curl -i http://localhost:5198/api/v1/me/certificates/$CERT \
  -H "X-Student-Id: 99999999-9999-4999-8999-999999999999"
```

Caso degradado — con Course Authoring apagado, ni se concede una matrícula nueva ni se registra
ninguna lección:

```bash
# Detener el proceso de Course Authoring (Ctrl+C en su terminal) y matricularse en otro curso
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d '{"courseId":"44444444-4444-4444-4444-444444444444"}'

# HTTP/1.1 503 Service Unavailable
# Retry-After: 5

# Marcar una leccion del curso publicado antes, con Course Authoring todavia apagado
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
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
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"
# HTTP/1.1 201 Created

curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"
# HTTP/1.1 404 Not Found     <- el mensaje aun no ha llegado

# Esperar unos segundos y repetir la misma peticion
curl -i -X POST http://localhost:5197/api/v1/me/course-progress/$COURSE/completed-lessons \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
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
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"lessonId\":\"$LESSON1\"}"
# {"status":"InProgress","completedLessonCount":1,"totalLessonCount":3,"percentage":33.33, ...}

# Consultarlo enseguida: todavia puede devolver el estado anterior
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
# {"completedLessonCount":0,"totalLessonCount":null,"percentage":null, ...}

# Repetir unos segundos despues: ya coincide
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
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
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
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
curl -s http://localhost:5198/api/v1/me/certificates -H "X-Student-Id: $STUDENT"
# []      <- el trabajo esta anotado, pero todavia no hay nada que emitir

# Arrancar Course Authoring y esperar unos segundos
curl -s http://localhost:5198/api/v1/me/certificates -H "X-Student-Id: $STUDENT"
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
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$COURSE\"}"
# HTTP/1.1 201 Created    <- la matricula existe

curl -i http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
# HTTP/1.1 404 Not Found  <- y seguira asi mientras el broker no vuelva
```

El mensaje queda guardado en la base de Enrollment, esperando. Se puede ver:

```bash
docker exec -e PGPASSWORD=enrollment_dev lms-postgres \
  psql -U enrollment_user -d enrollment \
  -c "SELECT id, published_at, attempt_count FROM outbox_messages WHERE published_at IS NULL"
```

Al volver a arrancar el broker, el progreso aparece solo, sin repetir ninguna petición:

```bash
docker compose start rabbitmq

# Esperar a que el contenedor este healthy y consultar de nuevo
curl -s http://localhost:5197/api/v1/me/course-progress/$COURSE -H "X-Student-Id: $STUDENT"
# {"status":"InProgress","startedAt":"...", ...}
```

Fíjate en el `startedAt`: es el instante en que se concedió la matrícula, no aquel en que el broker
se recuperó.

### Matricularse sin haber arrancado Learning

Learning puede estar apagado —incluso no haberse arrancado nunca— cuando alguien se matricula. El
mensaje espera en la cola:

```bash
# Detener Learning (Ctrl+C en su terminal) y matricularse en otro curso publicado
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d "{\"courseId\":\"$OTRO_COURSE\"}"
# HTTP/1.1 201 Created
```

En <http://localhost:15672>, la cola `lms.learning.student-enrolled` muestra un mensaje pendiente.
Al arrancar Learning otra vez, lo consume y el progreso aparece:

```bash
dotnet run --project src/services/learning/Learning.Api --launch-profile http

curl -s http://localhost:5197/api/v1/me/course-progress/$OTRO_COURSE -H "X-Student-Id: $STUDENT"
```

> **`X-Instructor-Id` y `X-Student-Id` no son autenticación.** Son un mecanismo temporal para poder
> ejecutar el proyecto en Development: cualquiera puede escribir cualquier GUID y actuar como esa
> persona. No expongas estos servicios fuera de tu máquina.

> **El usuario `lms` del broker es compartido y tiene permisos amplios.** Vale para Development y
> nada más: los dos servicios lo usan, y puede configurar, escribir y leer cualquier cosa. Un usuario
> por servicio con permisos mínimos es trabajo pendiente.

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

Cabecera obligatoria `X-Student-Id`. Sin `status` devuelve los cursos en progreso. El filtro no
distingue mayúsculas —`?status=completed` es lo mismo que `?status=Completed`—, igual que Learning;
cualquier otro valor, incluido el vacío, responde `400` sin llegar a llamar a Learning.

Continuando el recorrido de más arriba, con los cinco procesos arriba:

```bash
curl -s http://localhost:5199/api/v1/me/courses-in-progress -H "X-Student-Id: $STUDENT"
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
curl -s http://localhost:5199/api/v1/me/courses-in-progress -H "X-Student-Id: $STUDENT"
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
curl -i -s http://localhost:5199/api/v1/me/courses-in-progress -H "X-Student-Id: $STUDENT"
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
contraste entre los ≈ 0,1 s del `422` y los ≈ 3,0 s de la indisponibilidad es la prueba de que un
fallo funcional no se reintenta y uno transitorio sí.

> **El Circuit Breaker está configurado pero no llega a abrirse deteniendo un proceso.** En Windows,
> rechazar una conexión contra un puerto muerto tarda ≈ 2 s, así que el timeout total cancela la
> operación antes de que se acumulen los tres fallos que el breaker necesita, y una cancelación no
> se contabiliza como fallo. La configuración está puesta y el orden del pipeline es el correcto;
> demostrarlo abriéndose exige una dependencia que falle rápido, y es trabajo pendiente.

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

## Documentación

El diseño de la plataforma (contextos, decisiones técnicas y diagramas) está en
[`docs/`](./docs/arquitectura/vision-general.md).
