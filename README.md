# LMS — Plataforma de aprendizaje basada en microservicios (.NET 10)

> **Estado del proyecto:**
> - documentación y decisiones arquitectónicas.
---

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
| `course-authoring` | MVP | crear/editar cursos y lecciones, publicar, republicar, catálogo |
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

Verificación de que la tabla existe con sus cinco columnas:

```bash
docker exec lms-postgres psql -U postgres -d course_authoring -c "\d courses"
```

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

### 10.4 Probar los endpoints

```bash
# Crear un curso
curl -i -X POST http://localhost:5195/api/v1/courses \
  -H "X-Instructor-Id: 018f2c4a-0000-7000-8000-000000000001" \
  -H "Content-Type: application/json" \
  -d '{"title":"Microservicios con .NET 10"}'

# Consultarlo (sustituir por el id devuelto en la cabecera Location)
curl -i http://localhost:5195/api/v1/courses/<id>
```

`POST /api/v1/courses` devuelve `201` con cabecera `Location` y el curso en estado `Draft`.
Los errores viajan siempre como `application/problem+json`: `400` si el cuerpo es inválido o falta
la cabecera `X-Instructor-Id`, `404` si el curso no existe, `500` ante un fallo no controlado.

> **`X-Instructor-Id` no es un mecanismo de seguridad.** Es un tapón de desarrollo detrás de la
> abstracción `ICurrentActor`: cualquier cliente puede enviar el identificador que quiera. Cuando
> entre Keycloak (ADR-T15, incremento 11) se sustituye el adaptador por uno que lea el `sub` del
> token, sin tocar los casos de uso.

### 10.5 Ejecutar las pruebas

```bash
dotnet test LMS.sln
```

Dos proyectos: pruebas unitarias de dominio y pruebas de integración. **Las de integración exigen
Docker en marcha**: levantan su propio contenedor PostgreSQL con Testcontainers, le aplican las
migraciones y no tocan la base del `docker-compose`.

### 10.6 Detener el entorno

```bash
docker compose down       # conserva los datos
docker compose down -v    # elimina también el volumen
```

## 11. Roadmap por incrementos

| # | Incremento | Estado |
|---:|---|---|
| 1 | Documentación, ADR y diagramas | **Completado** |
| 2 | Course Authoring | **En curso** — base ejecutable |
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
| Código de servicios | **Parcial** — `course-authoring` compila, arranca y persiste; los otros siete no existen |
| Contenedores y manifiestos | **Parcial** — `docker-compose.yml` solo con PostgreSQL; sin `Dockerfile` de servicio ni Kubernetes |
| Pruebas | **Parcial** — unitarias de dominio e integración con Testcontainers en `course-authoring` |

### Alcance real de `course-authoring` hoy

**Implementado (SPEC 01):** agregado `Course` con identificadores fuertemente tipados ·
`POST /api/v1/courses` y `GET /api/v1/courses/{id}` con versionado en la ruta (ADR-T24) ·
persistencia con EF Core y PostgreSQL · OpenAPI y Scalar en
Development · errores en `application/problem+json` · `/health` de conectividad · logging
estructurado en consola.

**No implementado todavía:** entidad `Lesson` · acción `Publish` y estado `Published` · republicación
y copia de trabajo (ADR-0002) · catálogo · autorización real · eventos de dominio, Outbox y broker.

## 14. Secciones pendientes de este README

Se completarán en incrementos posteriores: ejecución del stack completo con Docker Compose ·
despliegue en Kubernetes · configuración de Keycloak · configuración del Gateway · configuración del
broker · observabilidad · colección de pruebas · troubleshooting.
