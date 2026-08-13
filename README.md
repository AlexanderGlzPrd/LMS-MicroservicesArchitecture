# LMS — Plataforma de aprendizaje basada en microservicios

Plataforma de formación donde un instructor crea cursos con lecciones, los publica y un público
general los consulta en un catálogo. El objetivo es construir, de forma incremental, una plataforma
de aprendizaje sobre una arquitectura de microservicios en .NET.

Hoy el repositorio contiene un microservicio ejecutable: **Course Authoring** (autoría de cursos,
publicación y catálogo público).

Un curso mantiene dos contenidos a la vez: el **contenido de trabajo**, que solo ve su instructor y
cambia con cada edición, y el **contenido publicado**, que ve el público en el catálogo y solo cambia
al publicar o republicar.

## Tecnologías

- .NET 10 · ASP.NET Core (controladores)
- Entity Framework Core 10 · Npgsql · PostgreSQL 17
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
docker compose up -d
docker compose ps          # esperar STATUS = healthy
```

Levanta el contenedor `lms-postgres` (PostgreSQL 17) en el puerto `5432`. En el primer arranque
ejecuta `deploy/postgres/init/01-course-authoring.sql`, que crea la base `course_authoring` y el
usuario de servicio `course_authoring_user`.

> El script de inicialización solo se ejecuta con el directorio de datos vacío. Si lo modificas,
> recrea el volumen: `docker compose down -v && docker compose up -d`.

### 2. Aplicar las migraciones

Paso obligatorio: la API no aplica migraciones al arrancar.

```bash
dotnet ef database update --project src/services/course-authoring/CourseAuthoring.Infrastructure
```

La fábrica de tiempo de diseño usa por defecto la cadena de conexión local. Para apuntar a otra base,
define `COURSE_AUTHORING_CONNECTION`:

```bash
# PowerShell
$env:COURSE_AUTHORING_CONNECTION = "Host=...;Database=...;Username=...;Password=..."
```

### 3. Arrancar el microservicio

```bash
dotnet run --project src/services/course-authoring/CourseAuthoring.Api --launch-profile http
```

Escucha en `http://localhost:5195` con `ASPNETCORE_ENVIRONMENT=Development`.

### 4. Documentación y estado

| Recurso | URL | Disponible en |
|---|---|---|
| Documentación interactiva (Scalar) | `http://localhost:5195/scalar/v1` | solo Development |
| Documento OpenAPI | `http://localhost:5195/openapi/v1.json` | solo Development |
| Estado del servicio | `http://localhost:5195/health` | siempre |

```bash
curl http://localhost:5195/health          # Healthy
```

`/health` comprueba únicamente la conectividad con PostgreSQL, no el estado del esquema: si te
saltas las migraciones, responde `Healthy` y el fallo aparece en la primera escritura.

### 5. Detener el entorno

```bash
docker compose down       # conserva los datos
docker compose down -v    # elimina también el volumen
```

## Uso

Los endpoints de negocio están bajo `/api/v1/`. Los de autoría exigen la cabecera `X-Instructor-Id`
(identificador del instructor, un GUID); los de catálogo son públicos.

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

Flujo completo — crear, publicar, editar sin que el público se entere, republicar:

```bash
INSTRUCTOR="11111111-1111-1111-1111-111111111111"

# 1. Crear el curso. Devuelve 201 y la cabecera Location
curl -i -X POST http://localhost:5195/api/v1/courses \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10"}'

COURSE="<id devuelto>"

# 2. Agregar una leccion
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Introduccion","description":"Que es un microservicio","videoUrl":"https://videos.example.com/1.mp4"}'

# 3. Publicar. Exige al menos una leccion
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/publish \
  -H "X-Instructor-Id: $INSTRUCTOR"

# 4. Consultar el catalogo publico, sin cabecera de instructor
curl -s http://localhost:5195/api/v1/catalog/courses

# 5. Modificar el contenido de trabajo. El catalogo NO cambia todavia
curl -i -X PATCH http://localhost:5195/api/v1/courses/$COURSE \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10 (edicion 2)"}'

# 6. Republicar. Devuelve changed: true y el catalogo ya refleja el cambio
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/republish \
  -H "X-Instructor-Id: $INSTRUCTOR"
```

Los errores se devuelven siempre como `application/problem+json`.

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

Las pruebas de integración levantan su propio contenedor PostgreSQL con Testcontainers, le aplican
las migraciones y no tocan la base del `docker-compose`.

## Documentación

El diseño de la plataforma (contextos, decisiones técnicas y diagramas) está en
[`docs/`](./docs/architecture/architecture-overview.md).
