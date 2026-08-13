# LMS — Plataforma de aprendizaje basada en microservicios

Plataforma de formación donde un instructor crea cursos con lecciones, los publica y un público
general los consulta en un catálogo; un estudiante puede matricularse gratuitamente en los cursos
publicados. El objetivo es construir, de forma incremental, una plataforma de aprendizaje sobre una
arquitectura de microservicios en .NET.

Hoy el repositorio contiene dos microservicios ejecutables:

| Servicio | Puerto | Qué hace |
|---|---|---|
| **Course Authoring** | `5195` | autoría de cursos, publicación y catálogo público |
| **Enrollment** | `5196` | matrícula gratuita de un estudiante en un curso publicado |

Cada uno tiene su propia base de datos y su propio usuario de PostgreSQL, sin acceso a la base del
otro. Enrollment no lee la base de Course Authoring: le pregunta por HTTP si el curso está publicado
antes de conceder una matrícula, y si no obtiene respuesta no matricula a nadie.

Un curso mantiene dos contenidos a la vez: el **contenido de trabajo**, que solo ve su instructor y
cambia con cada edición, y el **contenido publicado**, que ve el público en el catálogo y solo cambia
al publicar o republicar.

## Tecnologías

- .NET 10 · ASP.NET Core (controladores)
- Entity Framework Core 10 · Npgsql · PostgreSQL 17
- Llamada síncrona entre servicios con `HttpClient` y timeout acotado
- Versionado de API con `Asp.Versioning`
- OpenAPI + Scalar para documentación interactiva
- Docker Compose para la base de datos local
- xUnit y Testcontainers para las pruebas

## Requisitos

| Requisito | Versión | Para qué |
|---|---|---|
| .NET SDK | 10.0.302 o superior dentro de 10.0 (fijada en `global.json`) | compilar y ejecutar |
| Docker Desktop | reciente | PostgreSQL local y pruebas de integración |
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

### 1. Levantar PostgreSQL

```bash
docker compose down -v     # obligatorio si ya tenías el volumen de una versión anterior
docker compose up -d
docker compose ps          # esperar STATUS = healthy
```

Levanta el contenedor `lms-postgres` (PostgreSQL 17) en el puerto `5432`. En el primer arranque
ejecuta los scripts de `deploy/postgres/init/`, que crean:

| Base | Usuario de servicio | Contraseña local |
|---|---|---|
| `course_authoring` | `course_authoring_user` | `course_authoring_dev` |
| `enrollment` | `enrollment_user` | `enrollment_dev` |

Ambos scripts revocan el permiso de conexión que PostgreSQL concede por defecto a todos los roles,
así que cada usuario de servicio solo alcanza su propia base:

```bash
docker exec -e PGPASSWORD=enrollment_dev lms-postgres \
  psql -U enrollment_user -d course_authoring -c "SELECT 1"
# FATAL: permission denied for database "course_authoring"
```

> **`docker compose down -v` es necesario, no opcional**, si ya habías levantado el proyecto antes:
> los scripts de inicialización solo se ejecutan con el directorio de datos vacío, y la creación de
> la base `enrollment` y la revocación de permisos son parte de esa inicialización. Sin recrear el
> volumen, el segundo servicio no tiene dónde conectarse.

### 2. Aplicar las dos migraciones

Paso obligatorio: las API no aplican migraciones al arrancar.

```bash
dotnet ef database update --project src/services/course-authoring/CourseAuthoring.Infrastructure
dotnet ef database update --project src/services/enrollment/Enrollments.Infrastructure
```

Cada fábrica de tiempo de diseño usa por defecto su cadena de conexión local. Para apuntar a otra
base, define la variable correspondiente:

```bash
# PowerShell
$env:COURSE_AUTHORING_CONNECTION = "Host=...;Database=...;Username=...;Password=..."
$env:ENROLLMENT_CONNECTION       = "Host=...;Database=...;Username=...;Password=..."
```

### 3. Arrancar los dos servicios

Cada uno en su propia terminal:

```bash
dotnet run --project src/services/course-authoring/CourseAuthoring.Api --launch-profile http
dotnet run --project src/services/enrollment/Enrollments.Api --launch-profile http
```

Escuchan en `http://localhost:5195` y `http://localhost:5196`, con
`ASPNETCORE_ENVIRONMENT=Development`.

Enrollment necesita saber dónde está Course Authoring. Lo lee de `Services:CourseAuthoring:BaseUrl`,
que en Development apunta a `http://localhost:5195`, y **no arranca si falta**. Puedes ajustar la
llamada sin recompilar:

| Ajuste | Por defecto | Para qué |
|---|---|---|
| `Services:CourseAuthoring:BaseUrl` | `http://localhost:5195` | destino de la consulta al catálogo |
| `Services:CourseAuthoring:TimeoutSeconds` | `3` | cuánto se espera la respuesta |
| `Services:CourseAuthoring:RetryAfterSeconds` | `5` | valor de la cabecera `Retry-After` del `503` |

### 4. Documentación y estado

| Recurso | Course Authoring | Enrollment | Disponible en |
|---|---|---|---|
| Documentación interactiva (Scalar) | `:5195/scalar/v1` | `:5196/scalar/v1` | solo Development |
| Documento OpenAPI | `:5195/openapi/v1.json` | `:5196/openapi/v1.json` | solo Development |
| Estado del servicio | `:5195/health` | `:5196/health` | siempre |

```bash
curl http://localhost:5195/health          # Healthy
curl http://localhost:5196/health          # Healthy
```

`/health` comprueba únicamente la conectividad de cada servicio con su propia base, no el estado del
esquema: si te saltas las migraciones, responde `Healthy` y el fallo aparece en la primera escritura.
El `/health` de Enrollment tampoco cambia porque Course Authoring esté caído: la salud de una
dependencia no es la salud propia.

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

Flujo completo — publicar un curso, matricularse, repetir y ver el `200`:

```bash
INSTRUCTOR="11111111-1111-1111-1111-111111111111"
STUDENT="22222222-2222-2222-2222-222222222222"

# 1. Crear el curso en Course Authoring. Devuelve 201 con su id
curl -i -X POST http://localhost:5195/api/v1/courses \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10"}'

COURSE="<id devuelto>"

# 2. Agregar una leccion y publicar. Publicar exige al menos una leccion
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Introduccion","description":"Que es un microservicio","videoUrl":"https://videos.example.com/1.mp4"}'

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
```

Caso degradado — con Course Authoring apagado, una matrícula nueva no se concede:

```bash
# Detener el proceso de Course Authoring (Ctrl+C en su terminal) y matricularse en otro curso
curl -i -X POST http://localhost:5196/api/v1/enrollments \
  -H "X-Student-Id: $STUDENT" -H "Content-Type: application/json" \
  -d '{"courseId":"44444444-4444-4444-4444-444444444444"}'

# HTTP/1.1 503 Service Unavailable
# Retry-After: 5
```

No se crea ninguna fila. Enrollment prefiere no matricular a matricular sin haber comprobado que el
curso está publicado.

> **`X-Instructor-Id` y `X-Student-Id` no son autenticación.** Son un mecanismo temporal para poder
> ejecutar el proyecto en Development: cualquiera puede escribir cualquier GUID y actuar como esa
> persona. No expongas estos servicios fuera de tu máquina.

> **La llamada de Enrollment a Course Authoring solo tiene timeout.** No hay reintentos ni cortacircuitos:
> si Course Authoring tarda más de `TimeoutSeconds` o falla, la petición se resuelve con un `503`
> inmediato y el cliente decide si vuelve a intentarlo.

> Los cuerpos de ejemplo van sin acentos a propósito: pegar caracteres no ASCII en una terminal cuya
> página de códigos no es UTF-8 —el caso por defecto en Windows— los envía mal codificados. Si
> necesitas acentos, manda el cuerpo desde un archivo: `--data-binary @leccion.json`.

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

Las pruebas de integración levantan sus propios contenedores PostgreSQL con Testcontainers, les
aplican las migraciones y no tocan la base del `docker-compose`. Las de aislamiento arrancan además
un contenedor con los scripts reales de `deploy/postgres/init/` y comprueban que ninguno de los dos
usuarios de servicio puede conectarse a la base del otro.

## Documentación

El diseño de la plataforma (contextos, decisiones técnicas y diagramas) está en
[`docs/`](./docs/architecture/architecture-overview.md).
