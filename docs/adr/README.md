# Architectural Decision Records (ADR)

Registro de las decisiones arquitectónicas importantes del proyecto LMS.
Cada ADR resume, en ~1 página: **Contexto · Problema · Alternativas · Decisión · Justificación · Consecuencias**.

Existen **dos series** con propósitos distintos. **No se renumeran**: coexisten.

- **Serie `0001–…`** — decisiones **estratégicas y de dominio** (qué construimos y por qué).
- **Serie `T01–…`** — decisiones **técnicas y de arquitectura** (cómo lo construimos).

---

## Serie estratégica / dominio

| ADR | Título | Estado | Fecha |
|---|---|---|---|
| [0001](./0001-microservicios-como-driver-de-aprendizaje.md) | Microservicios como driver de aprendizaje | Aceptado | 2026-07-12 |
| [0002](./0002-edicion-de-cursos-y-finalizacion-como-hecho-historico.md) | Edición de cursos publicados y Finalización como hecho histórico | Aceptado | 2026-07-13 |
| [0003](./0003-clasificacion-subdominios.md) | Clasificación de subdominios (Core/Supporting/Generic) | Aceptado | 2026-07-13 |

## Serie técnica

| ADR | Título | Estado | Fecha |
|---|---|---|---|
| [T01](./ADR-T01-physical-microservices.md) | Microservicios físicos independientes | Aceptado | 2026-07-16 |
| [T02](./ADR-T02-monorepo.md) | Monorepo | Aceptado | 2026-07-16 |
| [T03](./ADR-T03-clean-architecture.md) | Clean Architecture por servicio | Aceptado | 2026-07-16 |
| [T04](./ADR-T04-database-per-service.md) | Database per Service | Aceptado | 2026-07-16 |
| [T05](./ADR-T05-postgresql-ef-core.md) | PostgreSQL con EF Core | Aceptado | 2026-07-16 |
| [T06](./ADR-T06-communication.md) | Comunicación síncrona y asíncrona | Aceptado | 2026-07-16 |
| [T07](./ADR-T07-rabbitmq-masstransit.md) | RabbitMQ con MassTransit | Aceptado | 2026-07-16 |
| [T08](./ADR-T08-transactional-outbox.md) | Transactional Outbox | Aceptado | 2026-07-16 |
| [T09](./ADR-T09-inbox-deduplication.md) | Inbox y deduplicación | Aceptado | 2026-07-16 |
| [T10](./ADR-T10-cqrs-learning.md) | CQRS en Learning | Aceptado | 2026-07-16 |
| [T11](./ADR-T11-api-composition.md) | API Composition en un BFF | Aceptado | 2026-07-16 |
| [T12](./ADR-T12-current-lesson-set.md) | Conjunto actual de LessonIds en Learning | Aceptado | 2026-07-16 |
| [T13](./ADR-T13-paid-enrollment-saga.md) | Saga académica: Compra de Acceso a Curso | **Aceptado con riesgos residuales** | 2026-07-16 |
| [T14](./ADR-T14-yarp-gateway.md) | API Gateway con YARP | Aceptado | 2026-07-16 |
| [T15](./ADR-T15-keycloak-security.md) | Seguridad con Keycloak (OAuth2/OIDC/JWT) | Aceptado | 2026-07-16 |
| [T16](./ADR-T16-observability.md) | Observabilidad | Aceptado | 2026-07-16 |
| [T17](./ADR-T17-docker-compose.md) | Docker y Docker Compose | Aceptado | 2026-07-16 |
| [T18](./ADR-T18-kubernetes.md) | Despliegue en Kubernetes | Aceptado | 2026-07-16 |
| [T19](./ADR-T19-resilience.md) | Políticas de resiliencia | Aceptado | 2026-07-16 |
| [T20](./ADR-T20-contract-versioning.md) | Contratos de mensajes y versionado | Aceptado | 2026-07-16 |
| [T21](./ADR-T21-testing-strategy.md) | Estrategia de pruebas | Aceptado | 2026-07-16 |
| [T22](./ADR-T22-technical-building-blocks.md) | Building blocks técnicos compartidos | Aceptado | 2026-07-16 |
| [T23](./ADR-T23-paid-enrollment-command.md) | Apertura mínima de Enrollment: ConcederMatriculaPorPagoCapturado | **Aceptado con riesgos residuales** | 2026-07-16 |

## Estados posibles
`Propuesto` · `Aceptado` · `Aceptado con riesgos residuales` · `Reemplazado por ADR-XXXX` · `Obsoleto`
