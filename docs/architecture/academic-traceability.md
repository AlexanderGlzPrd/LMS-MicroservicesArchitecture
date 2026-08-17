# Trazabilidad académica

- **Fecha:** 2026-08-17
- **Estado global:** cuatro servicios de dominio ejecutables sobre PostgreSQL y RabbitMQ, con el
  proceso completo `matricular → progresar → finalizar → certificar` en funcionamiento.

## Estados permitidos en esta fase

`Diseñado` · `Documentado` · `Implementado` · `Pendiente de implementación` · `Pendiente de prueba` · `Pendiente de evidencia`

> `Implementado` se marca solo sobre capacidad ejecutable y verificada a mano contra el entorno
> local. `Probado` y `Demostrable` siguen sin marcarse: la evidencia automatizada se concentra en
> el incremento final de validación.

---

## Curso 1 — Arquitectura de microservicios

| # | Criterio | Decisión | ADR | Evidencia prevista | Incr. | Estado |
|---:|---|---|---|---|---|---|
| 1 | Decompose by Business Capability | 4 servicios de dominio = 4 Bounded Contexts | T01 | diagrama general + C4 Container | 1 | Documentado |
| 2 | Domain Modeling / DDD | agregados, VO, invariantes, eventos de dominio | T03 | bounded-contexts.md + código de dominio | 1–6 | **Implementado** en los cuatro servicios: `Curso`, `Matricula`, `ProgresoDelCurso` y `Certificado`, con eventos de dominio internos en Learning y Certification |
| 3 | Database per Service | una base lógica por servicio, sin permisos cruzados | T04, T05 | technical-architecture.md + Compose/K8s | 1 | Documentado · Pendiente de implementación |
| 4 | Repository Pattern | un repositorio por Aggregate Root (puerto/adaptador) | T03 | código + prueba de integración | 2 | Diseñado · Pendiente de implementación |
| 5 | REST API Design | rutas, verbos, códigos, DTO, validaciones | T06 | application-flows.md + Swagger | 2–6 | **Implementado** en los cuatro servicios, incluida la verificación pública de certificados |
| 6 | API Composition | BFF con llamadas en paralelo | T11 | endpoint + escenario degradado **200 `isPartial`** | 9 | Diseñado · Pendiente de implementación |
| 7 | Resiliencia / Circuit Breaker | timeout, retry, CB, DLQ, fail-safe | T19 | captura de CB abierto + 503 | 13 | Diseñado · Pendiente de implementación |
| 8 | API Gateway | YARP como entrada única | T14 | configuración de rutas | 11 | Diseñado · Pendiente de implementación |
| 9 | Dockerfile por microservicio | contenerización de las 8 unidades | T17 | archivos de build | 12 | Diseñado · Pendiente de implementación |
| 10 | Docker Compose | stack completo local | T17 | ejecución en un comando | 12 | Diseñado · Pendiente de implementación |
| 11 | Flujo completo (Swagger/Postman) | 18 escenarios | T21 | colección + capturas | 16 | Diseñado · Pendiente de evidencia |
| 12 | Buenas prácticas / consistencia | sin base compartida, sin `Shared.Domain` | T22 | estructura del repositorio | 1 | Documentado |
| 13 | Diagrama general de arquitectura | diagrama con tecnologías y flujos | — | `docs/diagrams/architecture-overview.md` | 1 | **Documentado** |
| 14 | Estructura del repositorio | monorepo | T02 | árbol + README | 1 | Documentado |
| 15 | Historial de commits | Conventional Commits, un commit por capacidad | — | historial + tags | 1–16 | Pendiente de evidencia |
| 16 | Documentación | README + ADR + diagramas | — | `docs/` | 1 | **Documentado** |

## Curso 2 — Microservicios distribuidos, EDA, CQRS y Saga

| # | Criterio | Decisión | ADR | Evidencia prevista | Incr. | Estado |
|---:|---|---|---|---|---|---|
| 1 | Proceso de negocio distribuido | matricular → progresar → finalizar → certificar | T06 | diagramas de secuencia | 1 | Documentado |
| 2 | APIs REST por microservicio | endpoints alineados al negocio | T06 | Swagger | 2–6 | Diseñado |
| 3 | Arquitectura de microservicios | 6 servicios desacoplados, capas separadas | T01, T03 | C4 Container | 1 | Documentado |
| 4 | Event-Driven Architecture | RabbitMQ + MassTransit; **2 Integration Events con consumidor real** | T07 | diagramas de flujo + trazas | 5, 6 | **Implementado**: `StudentEnrolled` y `CourseCompleted`, dos productores con Outbox y dos consumidores idempotentes con Inbox |
| 5 | Contratos de Integration Events | versión en el tipo, cambios solo aditivos | T20 | `*.Contracts` + pruebas de contrato | 5 | Diseñado |
| 6 | **CQRS** | modelo de escritura y modelo de lectura en Learning | T10 | dos modelos + diagrama de componentes | ~~7~~ → **6** | **Implementado**: `course_progress` frente a `course_progress_view`, actualizada por eventos de dominio internos con consistencia eventual. Planificado para el incremento 7, se incorpora en el 6 |
| 7 | **Saga** | Compra de Acceso orquestada (**extensión académica**) | T13, T23 | diagrama de estados + reembolso ejecutado | 10 | Diseñado · Pendiente de implementación |
| 8 | Estados y transiciones | 17 estados documentados | T13 | `paid-enrollment-saga.md` | 1 | **Documentado** |
| 9 | Compensaciones | anulación antes de captura · reembolso después | T13 | escenario compensado | 10 | Diseñado |
| 10 | Consistencia eventual | Outbox/Inbox, propagación posterior | T08, T09 | traza extremo a extremo | 5 | Diseñado |
| 11 | Resiliencia y reintentos | backoff, DLQ, poison messages | T19 | mensaje en DLQ | 13 | Diseñado |
| 12 | Idempotencia | Inbox por `MessageId` · claves naturales · ledger por `PurchaseId` | T09, T23 | prueba de duplicados | 5, 10 | Diseñado |
| 13 | Excepciones y validaciones | manejo global de errores, ProblemDetails | T03 | respuestas de error | 2 | Diseñado |
| 14 | Logging estructurado | correlación con `TraceId`/`CorrelationId` | T16 | logs correlacionados | 14 | Diseñado |
| 15 | JWT | validación en Gateway **y** en cada servicio | T15 | 401/403 reales | 11 | Diseñado |
| 16 | Mínimo tres microservicios | seis servicios de aplicación | T01 | Compose/K8s | 1 | Documentado |
| 17 | Repositorio y README | monorepo documentado | T02 | `docs/` + README | 1 | **Documentado** |

## Curso 3 — Cloud-native, Kubernetes, seguridad y observabilidad

| # | Criterio | Decisión | ADR | Evidencia prevista | Incr. | Estado |
|---:|---|---|---|---|---|---|
| 1 | Arquitectura cloud-native | servicios sin estado, configuración externa, probes | T18 | manifiestos | 15 | Diseñado |
| 2 | Mínimo tres microservicios | seis | T01 | manifiestos | 1 | Documentado |
| 3 | Docker | 8 unidades contenerizadas | T17 | imágenes | 12 | Diseñado |
| 4 | Kubernetes: Deployments/Services/Pods | Deployment para servicios; StatefulSet para persistencias | T18 | manifiestos aplicados | 15 | Diseñado |
| 5 | Escalabilidad | HPA en `learning`, réplicas 1→3 | T18 | prueba de escalado con carga | 15 | Diseñado · Pendiente de prueba |
| 6 | OAuth2 / OIDC / JWT | Keycloak + validación en profundidad | T15 | flujo de token | 11 | Diseñado |
| 7 | Keycloak: realm, clientes, roles, usuarios | realm `lms` con 3 roles | T15 | export del realm | 11 | Diseñado |
| 8 | API Gateway | YARP con rutas públicas y por rol | T14 | configuración | 11 | Diseñado |
| 9 | Prometheus / Grafana / Jaeger | OpenTelemetry | T16 | dashboards + traza completa | 14 | Diseñado · Pendiente de evidencia |
| 10 | Logs y trazabilidad distribuida | correlación en HTTP y mensajería | T16 | logs por `TraceId` | 14 | Diseñado |
| 11 | Resiliencia y recuperación | eliminación de pod, DLQ, `ManualReview` | T18, T19 | capturas de diagnóstico | 15 | Diseñado · Pendiente de prueba |
| 12 | Troubleshooting | guía en README | — | sección de troubleshooting | 16 | Pendiente de evidencia |
| 13 | Repositorio, manifiestos, configuración, README | monorepo con `deploy/` | T02, T18 | árbol del repositorio | 15 | Diseñado |

---

## Criterios todavia sin evidencia

Escalado, recuperación ante fallos en Kubernetes, trazas distribuidas, dashboards, colección
Postman ejecutada, DLQ propia con mensaje real y compensación de Saga ejecutada. Quedan en
`Pendiente de implementación` / `Pendiente de prueba` / `Pendiente de evidencia`.
