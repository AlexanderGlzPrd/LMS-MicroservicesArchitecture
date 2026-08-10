# Arquitectura técnica

- **Fecha:** 2026-07-16 · **Estado:** congelado (T01–T23) · **Sin implementación todavía**

---

## 1. Unidades físicas

Ocho unidades desplegables: seis servicios de aplicación + Gateway + BFF.

| Unidad | Persistencia propia | API | Mensajería |
|---|---|---|---|
| `course-authoring` | PostgreSQL | REST | — |
| `enrollment` | PostgreSQL | REST | consume comando de Saga · produce `EstudianteMatriculado` + reply |
| `learning` | PostgreSQL (escritura + lectura) | REST | consume `EstudianteMatriculado` · produce `CursoFinalizado` |
| `certification` | PostgreSQL | REST | consume `CursoFinalizado` |
| `paid-enrollment` | PostgreSQL (estado de Saga) | REST | produce comandos · consume replies |
| `payment-provider-sim` | PostgreSQL (mínima) | — | consume comandos · produce replies |
| `gateway` (YARP) | — | routing | — |
| `bff-composition` | — | REST | — |

## 2. Arquitectura interna por servicio — Clean Architecture

```
<Service>.Api             controllers · DTO · validación · errores globales · health
<Service>.Application     Commands · Queries · Handlers · puertos (interfaces) · puertos ACL
<Service>.Domain          Aggregate Roots · Entities · Value Objects · Domain Events   ← sin dependencias
<Service>.Infrastructure  EF Core · repositorios · Outbox/Inbox · clientes ACL · messaging
<Service>.Contracts       Integration Events / mensajes publicados por ESTE servicio (DTO puros)
```

**Dirección de dependencias:** `Api → Application → Domain`; `Infrastructure → Application/Domain`.
El **dominio no depende de nada**.

**Regla dura de contratos:** un tipo de `*.Contracts` **no puede salir de Infrastructure/ACL**; el
consumidor lo mapea inmediatamente a su modelo interno.

## 3. Building blocks técnicos compartibles

Permitidos (**sin dominio**): `BuildingBlocks.Messaging` (sobre de mensaje, abstracciones de
Outbox/Inbox), `BuildingBlocks.Observability` (correlación, logging, OpenTelemetry),
`BuildingBlocks.Web` (middleware de errores, ProblemDetails, health checks), `BuildingBlocks.Testing`.

**Prohibido:** `Shared.Domain`, agregados, entidades, Value Objects de negocio, estados,
repositorios o cualquier clase de dominio compartida.

## 4. Persistencia

- **Motor:** PostgreSQL + EF Core.
- **Database per Service**: una base lógica por servicio, con usuario propio y **sin permisos cruzados**.
- **Docker Compose:** una instancia con una base y un usuario por servicio (equilibrio de recursos).
- **Kubernetes:** un StatefulSet + PVC por servicio (evidencia más fuerte).
- **Prohibido:** tablas compartidas, joins entre servicios, acceso a bases ajenas, base como contrato.

**Restricciones de unicidad que protegen invariantes de conjunto:**

| Regla | Mecanismo |
|---|---|
| una Matrícula por `(StudentId, CourseId)` | UNIQUE compuesto |
| un Progreso por `(StudentId, CourseId)` | clave primaria compuesta |
| un Certificado por Finalización | UNIQUE sobre la referencia de Finalización |
| un `PurchaseId` no reutilizable | UNIQUE en el ledger de Enrollment |

## 5. CQRS (solo en Learning)

- **Modelo de escritura:** agregado `ProgresoDelCurso`.
- **Modelo de lectura:** proyección con estado, nº de completadas y **%**, actualizada por eventos de
  dominio **internos**; misma base, tablas separadas; **consistencia eventual**.
- CQRS **no** implica Event Sourcing, **no** exige dos motores y **no** es renombrar métodos.

## 6. Resiliencia (resumen)

Timeout acotado · reintentos con backoff exponencial · Circuit Breaker en llamadas síncronas ·
Dead Letter Queue para mensajes inválidos o *poison* (**sin reintentar errores funcionales**) ·
health checks · **fail-safe** cuando una precondición externa no es verificable.

**Nunca:** reintentos ilimitados, Circuit Breaker en operaciones locales, compensar hechos irreversibles.

## 7. Observabilidad

OpenTelemetry → **Jaeger** (trazas), **Prometheus** (métricas), **Grafana** (dashboards), logging
estructurado. Correlación obligatoria: `TraceId`, `CorrelationId`, `CausationId`, `MessageId`,
`PurchaseId`. Métricas de negocio: matrículas, finalizaciones, certificados emitidos, **estados de
Saga y compensaciones ejecutadas**.

## 8. Despliegue

- **Docker Compose:** stack completo para ejecución local y prueba del flujo íntegro.
- **Kubernetes:** Deployment para servicios .NET, Gateway, BFF y **Keycloak** (su estado vive en
  PostgreSQL); StatefulSet + PVC para PostgreSQL, RabbitMQ y Prometheus; ConfigMaps, Secrets,
  readiness/liveness probes y HPA en `learning`.

> Desplegar bases de datos y broker **dentro del clúster** es una decisión **académica/local**, no
> una recomendación de producción (allí serían servicios administrados).
