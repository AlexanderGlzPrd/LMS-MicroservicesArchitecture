# LMS — Plataforma de aprendizaje basada en microservicios (.NET 10)

## 1. Caso de negocio

Plataforma de formación profesional y tecnológica donde un **Instructor** crea y publica cursos, un
**Estudiante** explora el catálogo, se matricula, marca lecciones como completadas, finaliza el curso
y obtiene un **certificado de finalización** que un tercero puede **verificar públicamente**.

## 2. Alcance

**Incluido en el MVP:** autoría y publicación de cursos · catálogo · matrícula gratuita ·
seguimiento del progreso · finalización · emisión y verificación de certificados.

**Fuera del MVP:** módulos dentro de un curso · evaluaciones y exámenes · despublicar cursos ·
desmatriculación · revocación de certificados · pagos en el flujo principal.

## 3. Microservicios

| Servicio | Ámbito | Responsabilidad |
|---|---|---|
| `course-authoring` | MVP — **implementado** | crear/editar cursos y lecciones, publicar, republicar, catálogo |
| `enrollment` | MVP | conceder acceso de un estudiante a un curso |
| `learning` | **MVP — Core Domain** | progreso del estudiante y sellado de la Finalización |
| `certification` | MVP | emisión y verificación de certificados |
| `paid-enrollment` | **extensión académica** | orquestador de la Saga de compra |
| `payment-provider-sim` | **extensión académica** | proveedor de pago simulado |
| `gateway` | técnico | punto de entrada único (YARP) |
| `bff-composition` | técnico | composición de vistas |

## 4. Extensión académica

La **Saga “Compra de Acceso a Curso”** existe para demostrar un proceso distribuido con
**compensaciones reales** (anulación de autorización y reembolso), requisito de la evaluación.

> **El flujo gratuito permanece independiente**: no depende de la extensión, no cambia su
> comportamiento y los contextos del MVP no la conocen. El flujo gratuito **es coreografía EDA, no
> una Saga**: sus hechos son irreversibles y no admiten compensación legítima.

## 5. Arquitectura

- Cuatro Bounded Contexts, un Aggregate Root por transacción, sin transacciones distribuidas.
- **Database per Service** con PostgreSQL y EF Core.
- Comunicación **síncrona** para verificaciones que exigen frescura y **asíncrona** (RabbitMQ) para
  hechos con consumidor obligatorio y para los pasos de la Saga que modifican estado.
- **Entrega at-least-once** con Inbox, claves naturales y restricciones de unicidad ⇒ **efecto de
  negocio effectively-once**.
- **No se publica ningún evento sin consumidor.**

Detalle: [docs/architecture/](./docs/architecture/architecture-overview.md)

## 6. Diagramas

| Diagrama | Archivo |
|---|---|
| Arquitectura general | [architecture-overview.md](./docs/diagrams/architecture-overview.md) |
| C4 Contexto | [c4-context.md](./docs/diagrams/c4-context.md) |
| C4 Contenedores | [c4-container.md](./docs/diagrams/c4-container.md) |
| Flujo Enrollment → Learning | [enrollment-learning-sequence.md](./docs/diagrams/enrollment-learning-sequence.md) |
| Flujo Learning → Certification | [learning-certification-sequence.md](./docs/diagrams/learning-certification-sequence.md) |
| Saga de compra | [paid-enrollment-saga.md](./docs/diagrams/paid-enrollment-saga.md) |

## 7. Decisiones

Índice completo en [docs/adr/README.md](./docs/adr/README.md): tres ADR estratégicos (`0001–0003`) y
veintitrés ADR técnicos (`T01–T23`).

Documentos de dominio previos: [Lenguaje Ubicuo](./docs/lenguaje-ubicuo.md) ·
[Subdominios](./docs/subdominios.md).

## 8. Tecnologías previstas

.NET · ASP.NET Core · EF Core · PostgreSQL · RabbitMQ con MassTransit · YARP · Keycloak ·
OpenTelemetry · Prometheus · Grafana · Jaeger · Docker · Docker Compose · Kubernetes.

## 9. Requisitos de ejecución

| Requisito | Versión | Para qué |
|---|---|---|
| .NET SDK | **10.0.302 o superior** dentro de 10.0 | compilar y ejecutar; la versión está fijada en `global.json` |
| Docker Desktop | cualquiera reciente | PostgreSQL local y pruebas de integración |
| `dotnet-ef` | 10.x | generar y aplicar migraciones |
| IDE | con soporte .NET 10 | Visual Studio 2026, VS Code + C# Dev Kit, o Rider |

```bash
dotnet --list-sdks                        # debe aparecer un 10.0.x
dotnet tool install --global dotnet-ef    # o: dotnet tool update --global dotnet-ef
```

> **Visual Studio 2022 no sirve.** Su MSBuild es 17.x y .NET 10 exige 18.x; al abrir la solución
> falla con `El SDK "Microsoft.NET.Sdk" especificado no se pudo encontrar`. Hay que usar
> Visual Studio 2026. La compilación por línea de comandos funciona con cualquier IDE instalado.

## 10. Ejecución local

Todo se ejecuta desde la raíz del repositorio. El orden **no es opcional**: la API no aplica
migraciones al arrancar (ver 10.2).

### 10.1 Levantar PostgreSQL

```bash
docker compose up -d
docker compose ps          # esperar STATUS = healthy
```

Levanta un único contenedor `lms-postgres` (PostgreSQL 17) en el puerto `5432`. En el primer
arranque ejecuta `deploy/postgres/init/01-course-authoring.sql`, que crea la base `course_authoring`
y el usuario de servicio `course_authoring_user` **sin privilegios administrativos** (ADR-T04):
`NOSUPERUSER`, `NOCREATEDB`, `NOCREATEROLE`, con `CONNECT` sobre su base y `USAGE` + `CREATE` sobre
su esquema `public`.

Comprobación de los privilegios del usuario de servicio:

```bash
docker exec lms-postgres psql -U postgres -d postgres -c \
  "SELECT rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname = 'course_authoring_user';"

docker exec lms-postgres psql -U postgres -d course_authoring -c \
  "SELECT has_database_privilege('course_authoring_user','course_authoring','CONNECT') AS db_connect,
          has_schema_privilege('course_authoring_user','public','USAGE')               AS schema_usage,
          has_schema_privilege('course_authoring_user','public','CREATE')              AS schema_create;"
```

Los tres primeros deben ser `f`; los tres siguientes, `t`.

> El script de inicialización **solo se ejecuta con el directorio de datos vacío**. Si lo modificas,
> hay que recrear el volumen: `docker compose down -v && docker compose up -d`.

### 10.2 Aplicar las migraciones — prerrequisito obligatorio

```bash
dotnet ef database update --project src/services/course-authoring/CourseAuthoring.Infrastructure
```

**Las migraciones se aplican siempre a mano.** No hay `Database.Migrate()` en el arranque: la
estrategia de despliegue se decide con Docker y Kubernetes (incrementos 12 y 15).

> **Si te saltas este paso, la API arranca igualmente y `GET /health` responde `Healthy`**, porque la
> comprobación de salud verifica únicamente la **conectividad** con PostgreSQL, no el estado del
> esquema. El fallo aparecerá en el primer `POST /api/v1/courses`, devuelto como
> `application/problem+json`.

Verificación del esquema. `courses` tiene ocho columnas (las cinco iniciales más `published_title`,
`published_at` y `published_content_updated_at`), y existen las dos tablas de lecciones:

```bash
docker exec lms-postgres psql -U postgres -d course_authoring -c "\d courses"
docker exec lms-postgres psql -U postgres -d course_authoring -c "\d lessons"
docker exec lms-postgres psql -U postgres -d course_authoring -c "\d published_lessons"
```

`lessons` guarda el **contenido de trabajo** y `published_lessons` el **snapshot publicado**. Ambas
con las mismas seis columnas, clave foránea a `courses.id` con borrado en cascada e índice
`(course_id, position)` **no único**: la contigüidad de las posiciones la garantiza el agregado, no
la base de datos (una restricción única fallaría a mitad de un reordenamiento).

La fábrica de tiempo de diseño usa por defecto la cadena de conexión local. Para apuntar a otra base:

```bash
# PowerShell
$env:COURSE_AUTHORING_CONNECTION = "Host=...;Database=...;Username=...;Password=..."
```

### 10.3 Arrancar la API

```bash
dotnet run --project src/services/course-authoring/CourseAuthoring.Api --launch-profile http
```

Queda escuchando en `http://localhost:5195` con `ASPNETCORE_ENVIRONMENT=Development`.

| Recurso | URL | Disponible en |
|---|---|---|
| Documentación interactiva (Scalar) | `http://localhost:5195/scalar/v1` | **solo Development** |
| Documento OpenAPI | `http://localhost:5195/openapi/v1.json` | **solo Development** |
| Estado del servicio | `http://localhost:5195/health` | siempre |

Todos los endpoints de negocio llevan la versión en la ruta: `/api/v{n}/...` (ADR-T24). Cada versión
publica su propio documento OpenAPI en `/openapi/v{n}.json` y las respuestas incluyen la cabecera
`api-supported-versions`. `/health` queda fuera del versionado: es un contrato operativo, no de negocio.

### 10.4 Contenido de trabajo y contenido publicado

Es la idea central del servicio y conviene entenderla antes de tocar los endpoints.

Un curso tiene **dos contenidos a la vez**:

| | Contenido de trabajo | Contenido publicado |
|---|---|---|
| Dónde vive | `courses.title` + tabla `lessons` | `courses.published_title` + tabla `published_lessons` |
| Quién lo ve | solo el instructor propietario, por `/api/v1/courses/**` | cualquiera, por `/api/v1/catalog/**` |
| Cuándo cambia | en cada edición, al instante | **solo al publicar o republicar** |

Editar un curso ya publicado —renombrarlo, corregir una lección, añadir otra, reordenarlas— **no
cambia nada de lo que ve el público**. El catálogo sigue mostrando el contenido anterior hasta que
el instructor ejecuta `POST /republish`. Esa es la garantía de ADR-0002 y el motivo de que existan
dos tablas en vez de una con un filtro que alguien pueda olvidar.

Una lección publicada **conserva el mismo identificador** que su lección de trabajo de origen: al
republicar se actualizan las que siguen existiendo, se insertan las nuevas y se borran las que
desaparecieron. No hay historial: solo existe la última versión publicada.

### 10.5 Endpoints

Todas las rutas de negocio bajo `/api/v1/` (ADR-T24).

**Autoría** — exigen la cabecera `X-Instructor-Id`:

| Método y ruta | Éxito | Qué hace |
|---|---|---|
| `POST /api/v1/courses` | `201` | crea el curso en `Draft`, con cabecera `Location` |
| `GET /api/v1/courses` | `200` | lista los cursos del actor, **sin paginar** |
| `GET /api/v1/courses/{id}` | `200` | detalle con el contenido de trabajo completo |
| `PATCH /api/v1/courses/{id}` | `200` | renombra el contenido de trabajo |
| `POST /api/v1/courses/{id}/lessons` | `201` | añade una lección al final |
| `PUT /api/v1/courses/{id}/lessons/{lessonId}` | `200` | edita una lección |
| `DELETE /api/v1/courses/{id}/lessons/{lessonId}` | `204` | elimina y recompacta posiciones a `1..N` |
| `PUT /api/v1/courses/{id}/lessons/order` | `200` | reordena por lote con la lista completa |
| `POST /api/v1/courses/{id}/publish` | `200` | `Draft → Published`; exige ≥1 lección |
| `POST /api/v1/courses/{id}/republish` | `200` | reemplaza el snapshot, o **no-op** si nada cambió |

**Catálogo** — público, sin cabecera:

| Método y ruta | Éxito | Qué hace |
|---|---|---|
| `GET /api/v1/catalog/courses?page=1&pageSize=20` | `200` | listado paginado de cursos publicados |
| `GET /api/v1/catalog/courses/{id}` | `200` | detalle del contenido publicado |

`pageSize` por defecto 20 y máximo 100; `page` empieza en 1. El orden es `published_at DESC, id ASC`
y **republicar no devuelve el curso al principio del listado**: ordenar por actualización reciente
sería una regla de ranking, y el Catálogo no tiene reglas propias en el MVP.

#### Los dos listados devuelven resúmenes, no detalles

Ninguno de los dos incluye `lessons`: listar diez cursos no debe arrastrar sus cien lecciones para
pintar diez títulos. El detalle vive en los endpoints por identificador.

`GET /api/v1/courses` — array JSON plano, sin envoltorio de paginación. Sin `instructorId`: todos
los cursos de la respuesta son del actor.

```json
[
  {
    "id": "019ff7b4-74b1-7d1e-aad3-de34f22bd45e",
    "title": "Microservicios con .NET 10",
    "status": "Published",
    "createdAt": "2026-08-12T20:40:26.545290+00:00",
    "publishedAt": "2026-08-12T20:40:44.476598+00:00",
    "publishedContentUpdatedAt": "2026-08-12T20:51:42.488948+00:00"
  }
]
```

`publishedAt` y `publishedContentUpdatedAt` son `null` mientras el curso esté en `Draft`.

`GET /api/v1/catalog/courses` — envuelto en un objeto de paginación con exactamente cuatro campos.
Todo sale del snapshot: `title` es `published_title` y `lessonCount` cuenta `published_lessons`.
Aquí las dos fechas **nunca** son `null`, porque un curso sin publicar no aparece.

```json
{
  "items": [
    {
      "id": "019ff7b4-74b1-7d1e-aad3-de34f22bd45e",
      "title": "Microservicios con .NET 10",
      "instructorId": "11111111-1111-1111-1111-111111111111",
      "lessonCount": 3,
      "publishedAt": "2026-08-12T20:40:44.476598+00:00",
      "publishedContentUpdatedAt": "2026-08-12T20:51:42.488948+00:00"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

La diferencia es deliberada: el listado del instructor no se pagina y el del catálogo sí desde el
primer día. Envolver después un array plano sería un cambio rompiente.

### 10.6 Recorrido completo con `curl`

Reproduce el circuito entero: crear, publicar, editar sin que el público se entere, y republicar.

```bash
INSTRUCTOR="11111111-1111-1111-1111-111111111111"

# 1. Crear el curso. Devuelve 201, Location y lessons: []
curl -i -X POST http://localhost:5195/api/v1/courses \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10"}'

COURSE="<id devuelto>"

# 2. Añadir dos lecciones. La posición la asigna el agregado: 1 y 2
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Introduccion","description":"Que es un microservicio","videoUrl":"https://videos.example.com/1.mp4"}'

curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Bounded Contexts","description":"Contextos delimitados","videoUrl":"https://videos.example.com/2.mp4"}'

# 3. Reordenar por lote: la lista completa, no un movimiento suelto
curl -i -X PUT http://localhost:5195/api/v1/courses/$COURSE/lessons/order \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"lessonIds":["<id-leccion-2>","<id-leccion-1>"]}'

# 4. Publicar
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/publish \
  -H "X-Instructor-Id: $INSTRUCTOR"

# 5. Ya está en el catálogo público. Fíjate: sin cabecera de instructor
curl -s http://localhost:5195/api/v1/catalog/courses
curl -s http://localhost:5195/api/v1/catalog/courses/$COURSE

# 6. Editar el contenido de trabajo: renombrar y añadir una lección
curl -i -X PATCH http://localhost:5195/api/v1/courses/$COURSE \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10 (edicion 2)"}'

curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/lessons \
  -H "X-Instructor-Id: $INSTRUCTOR" -H "Content-Type: application/json" \
  -d '{"title":"Outbox","description":"Patron Outbox","videoUrl":"https://videos.example.com/3.mp4"}'

# 7. El catálogo NO se ha enterado: mismo título, mismo lessonCount
curl -s http://localhost:5195/api/v1/catalog/courses

# 8. Republicar. Devuelve changed: true
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/republish \
  -H "X-Instructor-Id: $INSTRUCTOR"

# 9. Ahora sí: título nuevo y lessonCount actualizado. publishedAt no ha cambiado
curl -s http://localhost:5195/api/v1/catalog/courses

# 10. Republicar sin cambios es un no-op: changed: false y no se escribe nada
curl -i -X POST http://localhost:5195/api/v1/courses/$COURSE/republish \
  -H "X-Instructor-Id: $INSTRUCTOR"
```

`publishedAt` es la **primera** publicación y no cambia al republicar.
`publishedContentUpdatedAt` es la **última republicación con cambios**.

> Los cuerpos de ejemplo van sin acentos a propósito. La API acepta UTF-8 sin problema, pero pegar
> caracteres no ASCII dentro de comillas en una terminal cuya página de códigos no es UTF-8 —el caso
> por defecto en Windows— los envía mal codificados y la petición se rechaza con `400`. Si necesitas
> acentos, manda el cuerpo desde un archivo: `--data-binary @leccion.json`.

### 10.7 Códigos de estado

Los errores viajan siempre como `application/problem+json`.

| Código | Cuándo |
|---|---|
| `400` | forma o validez de los datos de entrada: cuerpo mal formado, campo obligatorio ausente, longitud excedida, `X-Instructor-Id` ausente o mal formada, `page`/`pageSize` fuera de rango, título o descripción vacíos, `videoUrl` no absoluta o sin esquema `http`/`https` |
| `403` | el actor no es el instructor propietario del curso |
| `404` | curso inexistente, lección inexistente en el curso, o detalle de catálogo de un curso no publicado |
| `409` | conflicto de estado del ciclo de vida: `publish` sobre un curso ya publicado, `republish` sobre un borrador |
| `422` | petición estructuralmente válida que el estado actual del curso no permite ejecutar: publicar o republicar sin lecciones, o lista de reordenamiento que no es una permutación exacta |
| `500` | fallo no controlado; el detalle no sale al cliente, queda en el log |

**`400` frente a `422`.** La diferencia no es qué capa detecta el problema, sino **de dónde procede
la insuficiencia**:

- **`400`** — la petición está mal escrita y lo estaría **contra cualquier estado del sistema**. Un
  título vacío o una URL relativa se rechazan sin mirar la base de datos. Que el dominio defienda
  además esa misma condición no la convierte en `422`: la doble defensa es deliberada, porque el
  dominio no confía en la API.
- **`422`** — la petición está bien escrita y **solo es inejecutable contra el estado actual** de ese
  curso. La misma petición valdría un minuto antes o después: añadir una lección hace que el
  `publish` funcione; enviar la lista completa hace que el reordenamiento funcione.

`GET /api/v1/catalog/courses/{id}` de un curso en `Draft` devuelve **`404`, no `403`**: para el
público ese curso no existe.

> **`X-Instructor-Id` no es un mecanismo de seguridad.** Es un tapón de desarrollo detrás de la
> abstracción `ICurrentActor`: cualquier cliente puede enviar el identificador que quiera. El `403`
> que devuelve un instructor ajeno es una **invariante del agregado**, no autorización. Cuando entre
> Keycloak (ADR-T15, incremento 11) se sustituye el adaptador por uno que lea el `sub` del token,
> sin tocar el dominio ni los casos de uso.

### 10.8 El contrato v1 está congelado

Las rutas sin versión de la etapa inicial (`POST /courses`, `GET /courses/{id}`) **no existen y no se
mantienen como alias**: devuelven `404`. Fueron un contrato bootstrap interno, previo a cualquier
consumidor, y ADR-T24 las trasladó a `/api/v1/` sin compatibilidad hacia atrás. Ese salto fue un
cambio rompiente deliberado, hecho en la única ventana en la que salía gratis.

A partir de aquí, `/api/v1` **solo admite cambios aditivos**: nuevos endpoints y nuevos campos
opcionales. Eliminar o renombrar un campo, cambiar su tipo o cambiar el significado de un código de
estado exige `v2`.

`/health`, `/openapi` y Scalar quedan fuera del versionado: son contratos operativos, no de negocio.

### 10.9 Ejecutar las pruebas

```bash
dotnet test LMS.sln
```


| Proyecto | Qué prueba | Necesita Docker |
|---|---|---|
| `CourseAuthoring.Domain.Tests` | invariantes del agregado: propiedad, posiciones, publicación y no-op | no |
| `CourseAuthoring.Application.Tests` | orquestación y autorización con dobles de los puertos | no |
| `CourseAuthoring.Integration.Tests` | repositorio, proyecciones del catálogo y la API completa | **sí** |

Las de integración levantan su propio contenedor PostgreSQL con Testcontainers, le aplican las
migraciones y **no tocan la base del `docker-compose`**. Las de API arrancan la aplicación real con
`WebApplicationFactory` contra ese contenedor. Sin SQLite y sin proveedor InMemory: la separación
entre contenido de trabajo y publicado se apoya en dos tablas y en proyecciones SQL, que un proveedor
en memoria no reproduce.

### 10.10 Detener el entorno

```bash
docker compose down       # conserva los datos
docker compose down -v    # elimina también el volumen
```

## 11. Roadmap por incrementos

| # | Incremento | Estado |
|---:|---|---|
| 1 | Documentación, ADR y diagramas | **Completado** |
| 2 | Course Authoring | **Completado** — lecciones, publicación, republicación y catálogo |
| 3 | Enrollment | Pendiente |
| 4 | Learning | Pendiente |
| 5 | Broker y flujo Enrollment → Learning | Pendiente |
| 6 | Certification y flujo Learning → Certification | Pendiente |
| 7 | CQRS en Learning | Pendiente |
| 8 | Resiliencia del conjunto de lecciones | Pendiente |
| 9 | API Composition (BFF) | Pendiente |
| 10 | Saga de compra | Pendiente |
| 11 | Gateway y Keycloak | Pendiente |
| 12 | Docker Compose | Pendiente |
| 13 | Políticas de resiliencia | Pendiente |
| 14 | Observabilidad | Pendiente |
| 15 | Kubernetes | Pendiente |
| 16 | Pruebas y evidencias | Pendiente |

## 12. Requisitos académicos

La trazabilidad completa de los criterios de los tres cursos está en
[academic-traceability.md](./docs/architecture/academic-traceability.md).
En esta fase el estado máximo posible es **Diseñado / Documentado / Pendiente**: no se marca ningún
criterio como implementado, probado ni demostrable.

## 13. Estado de implementación

| Aspecto | Estado |
|---|---|
| Documentación y ADR | **Documentado** |
| Diagramas iniciales | **Documentado** |
| Código de servicios | **Parcial** — `course-authoring` completo en su alcance de MVP; los otros siete no existen |
| Contenedores y manifiestos | **Parcial** — `docker-compose.yml` solo con PostgreSQL; sin `Dockerfile` de servicio ni Kubernetes |
| Pruebas | **Parcial** — tres niveles en `course-authoring`: dominio, aplicación e integración con Testcontainers |

### Alcance real de `course-authoring` hoy

| Aspecto | Estado |
|---|---|
| Agregado `Course` con identificadores fuertemente tipados | **Implementado** |
| Entidad `Lesson` dentro del agregado, con posición contigua `1..N` | **Implementado** |
| Edición del contenido de trabajo: añadir, editar, eliminar, reordenar por lote, renombrar | **Implementado** |
| Invariante de propiedad como regla de dominio (`403`) | **Implementado** |
| `Publish` con invariante de ≥1 lección | **Implementado** |
| `Republish` con comparación estructural y no-op | **Implementado** |
| Snapshot publicado en tabla separada (ADR-0002) | **Implementado** |
| Catálogo público paginado, listado y detalle | **Implementado** |
| Contrato HTTP v1 congelado (ADR-T24) y errores `problem+json` | **Implementado** |
| Persistencia con EF Core y PostgreSQL, migraciones a mano | **Implementado** |
| OpenAPI y Scalar en Development, `/health`, logging estructurado | **Implementado** |
| Eventos `CoursePublished` y `PublishedContentModified` | **Registrados en el agregado, sin despachar** |
| Despachador de eventos, `Contracts`, Outbox, Inbox, RabbitMQ | **Pendiente** |
| Autenticación real con Keycloak, Gateway, BFF | **Pendiente** |
| Despublicar y borrar cursos, historial de versiones, módulos, quizzes | **Fuera del MVP** |
| Búsqueda, filtros y ranking en el catálogo | **Fuera del MVP** |
| Concurrencia optimista sobre el agregado | **Pendiente** |
| `Dockerfile` del servicio, compose completo, Kubernetes, observabilidad | **Pendiente** |


## 14. Secciones pendientes de este README

Se completarán en incrementos posteriores: ejecución del stack completo con Docker Compose ·
despliegue en Kubernetes · configuración de Keycloak · configuración del Gateway · configuración del
broker · observabilidad · colección de pruebas · troubleshooting.
