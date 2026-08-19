# Estado del proyecto

- **Fecha:** 2026-08-18
- **Resumen:** cinco servicios de dominio ejecutables sobre PostgreSQL y RabbitMQ, más un proveedor
  de pago simulado y un BFF de composición. El proceso completo
  `matricular → progresar → finalizar → certificar` funciona extremo a extremo, y la compra de
  acceso lo hace por su propia Saga orquestada con compensaciones reales.

`Hecho` se marca solo sobre capacidad ejecutable y verificada a mano contra el entorno local.

---

## Hecho

| Área | Notas | ADR |
|---|---|---|
| Modelo de dominio | `Curso`, `Matricula`, `ProgresoDelCurso` y `Certificado`, con agregados, objetos de valor, invariantes y eventos de dominio internos en Learning y Certification | T03 |
| API REST de los cinco servicios | rutas, verbos, códigos, DTO y validaciones, incluidas la verificación pública de certificados y la compra de acceso | T06, T24 |
| Repositorios | uno por raíz de agregado, puerto en Application y adaptador en Infrastructure | T03 |
| Mensajería | RabbitMQ y MassTransit con dos Integration Events reales: `StudentEnrolled` y `CourseCompleted` | T07 |
| Outbox e Inbox | dos productores con Outbox transaccional y dos consumidores idempotentes con Inbox por `MessageId` | T08, T09 |
| CQRS en Learning | `course_progress` frente a `course_progress_view`, actualizada por eventos de dominio internos con consistencia eventual | T10 |
| Composición de API | `bff-composition` en el puerto 5199 con `GET /api/v1/me/courses-in-progress`, degradación parcial declarada en `isPartial` y `warnings[]`, y `503` con `Retry-After` cuando cae la dependencia esencial | T11 |
| Resiliencia HTTP | pipeline explícito timeout → retry → Circuit Breaker en las cinco llamadas síncronas, con `ExecutionRejectedException` capturada y fail-safe preservado | T19 |
| Manejo global de errores | ProblemDetails en los cinco procesos, con un único punto de traducción de excepciones | T03 |
| Base por servicio | una base lógica y un usuario por servicio, sin permisos cruzados, verificado con pruebas de aislamiento | T04, T05 |
| Monorepo | estructura documentada, sin base compartida y sin `Shared.Domain` | T02, T22 |
| Saga de compra de acceso | `paid-enrollment` y `payment-provider-sim` en 5200 y 5201, con los 17 estados, compensación real —anulación antes de captura, reembolso después—, reconciliación por consulta de estado durable y `ManualReview` con sus cuatro resoluciones | T13, T23, T28 |
| Idempotencia de extremo a extremo | tres capas —Inbox por `MessageId`, clave de negocio y estado de la Saga— con validación de correlación previa a cualquier escritura | T09, T13 |
| Diagramas y decisiones | C4 de contexto y contenedores, secuencias, y los ADR de `docs/adr/` | — |

## Parcial

| Área | Qué falta | ADR |
|---|---|---|
| Resiliencia en mensajería | el backoff exponencial HTTP ya existe —200 ms y 400 ms, sin jitter, acotado por el timeout total—. Faltan DLQ propia y tratamiento de poison messages | T19 |
| Contratos de Integration Events | versión en el tipo y cambios solo aditivos están decididos y aplicados; faltan pruebas de contrato | T20 |
| Logging | logs estructurados en JSON en los siete procesos, con `saga-correlation-mismatch` y `saga-late-message` en el orquestador; falta la correlación por `TraceId` entre HTTP y mensajería | T16 |
| Retención de tablas técnicas | `inbox_messages`, `outbox_messages`, `purchase_grants` y `purchase_resolutions` crecen sin límite; la política de purga está declarada, no implementada | T08, T09 |

## Pendiente

| Área | Notas | ADR |
|---|---|---|
| API Gateway | YARP como entrada única, con rutas públicas y por rol | T14 |
| Seguridad | Keycloak con realm `lms` y tres roles; validación de JWT en el gateway y en cada servicio, sustituyendo `X-Instructor-Id` y `X-Student-Id` | T15 |
| Observabilidad | OpenTelemetry con Prometheus, Grafana y Jaeger | T16 |
| Contenerización | Dockerfile por unidad y stack completo en Docker Compose | T17 |
| Kubernetes | Deployments y Services para los servicios, StatefulSet para las persistencias, probes y HPA en `learning` | T18 |
| Colección de escenarios | recorrido completo ejecutable desde Postman | T21 |
| Guía de diagnóstico | sección de troubleshooting en el README | — |
