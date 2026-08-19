# Architectural Decision Records

Registro de las decisiones arquitectónicas del LMS. Cada ADR resume en una página:
**Contexto · Problema · Alternativas · Decisión · Justificación · Consecuencias**.

Serie única y correlativa. Un ADR aceptado **no se edita**: si una decisión posterior lo corrige o
acota su alcance, se registra en un ADR nuevo que enlaza al anterior.

| ADR | Título | Estado | Fecha |
|---|---|---|---|
| [0001](./ADR-0001-estilo-arquitectonico-microservicios.md) | Estilo arquitectónico basado en microservicios | Aceptado | 2026-07-12 |
| [0002](./ADR-0002-edicion-de-cursos-y-finalizacion-inmutable.md) | Edición de cursos publicados y Finalización como hecho histórico | Aceptado | 2026-07-13 |
| [0003](./ADR-0003-clasificacion-de-subdominios.md) | Clasificación de subdominios (Core/Supporting/Generic) | Aceptado | 2026-07-13 |
| [0004](./ADR-0004-servicios-desplegables-por-contexto.md) | Un servicio desplegable por Bounded Context | Aceptado | 2026-07-16 |
| [0005](./ADR-0005-monorepo.md) | Monorepo | Aceptado | 2026-07-16 |
| [0006](./ADR-0006-clean-architecture-por-servicio.md) | Clean Architecture por servicio | Aceptado | 2026-07-16 |
| [0007](./ADR-0007-base-de-datos-por-servicio.md) | Database per Service | Aceptado | 2026-07-16 |
| [0008](./ADR-0008-postgresql-y-ef-core.md) | PostgreSQL con EF Core | Aceptado | 2026-07-16 |
| [0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) | Comunicación síncrona y asíncrona | Aceptado | 2026-07-16 |
| [0010](./ADR-0010-rabbitmq-y-masstransit.md) | RabbitMQ con MassTransit | Aceptado | 2026-07-16 |
| [0011](./ADR-0011-outbox-transaccional.md) | Transactional Outbox | Aceptado | 2026-07-16 |
| [0012](./ADR-0012-inbox-y-deduplicacion.md) | Inbox y deduplicación | Aceptado | 2026-07-16 |
| [0013](./ADR-0013-cqrs-en-learning.md) | CQRS en Learning | Aceptado | 2026-07-16 |
| [0014](./ADR-0014-composicion-de-api-en-bff.md) | API Composition en un BFF | Aceptado | 2026-07-16 |
| [0015](./ADR-0015-conjunto-vigente-de-lecciones.md) | Conjunto actual de LessonIds en Learning | Aceptado | 2026-07-16 |
| [0016](./ADR-0016-saga-de-compra-de-acceso.md) | Saga de compra de acceso a curso | **Aceptado con riesgos residuales** | 2026-07-16 |
| [0017](./ADR-0017-api-gateway-con-yarp.md) | API Gateway con YARP | Aceptado | 2026-07-16 |
| [0018](./ADR-0018-seguridad-con-keycloak.md) | Seguridad con Keycloak (OAuth2 / OIDC / JWT) | Aceptado | 2026-07-16 |
| [0019](./ADR-0019-observabilidad.md) | Observabilidad | Aceptado | 2026-07-16 |
| [0020](./ADR-0020-docker-y-docker-compose.md) | Docker y Docker Compose | Aceptado | 2026-07-16 |
| [0021](./ADR-0021-despliegue-en-kubernetes.md) | Despliegue en Kubernetes | Aceptado | 2026-07-16 |
| [0022](./ADR-0022-politicas-de-resiliencia.md) | Políticas de resiliencia | Aceptado | 2026-07-16 |
| [0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) | Contratos de mensajes y versionado | Aceptado | 2026-07-16 |
| [0024](./ADR-0024-estrategia-de-pruebas.md) | Estrategia de pruebas | Aceptado | 2026-07-16 |
| [0025](./ADR-0025-building-blocks-tecnicos.md) | Building blocks técnicos compartidos | Aceptado | 2026-07-16 |
| [0026](./ADR-0026-concesion-de-matricula-por-pago-capturado.md) | Apertura mínima de Enrollment: ConcederMatriculaPorPagoCapturado | **Aceptado con riesgos residuales** | 2026-07-16 |
| [0027](./ADR-0027-versionado-de-apis-rest.md) | Versionado de las APIs REST | Aceptado | 2026-08-12 |
| [0028](./ADR-0028-retirada-de-la-creacion-implicita-de-progreso.md) | Retirada de la creación provisional del Progreso | Aceptado | 2026-08-15 |
| [0029](./ADR-0029-puerto-de-directorio-de-estudiantes.md) | Fuente provisional del nombre del estudiante en Certification | Aceptado | 2026-08-16 |
| [0030](./ADR-0030-progreso-sin-snapshot-de-lecciones.md) | `ProgresoDelCurso` no persiste snapshot de `LessonIds` | Aceptado | 2026-08-16 |
| [0031](./ADR-0031-tipo-de-matricula-pagada.md) | Alcance de la cláusula «no se modifica el Aggregate Root» de ADR-0026 | Aceptado | 2026-08-18 |
| [0032](./ADR-0032-consulta-de-acceso-por-cuenta-de-servicio.md) | Consulta de acceso de Enrollment autorizada por cuenta de servicio | Aceptado | 2026-08-19 |
| [0033](./ADR-0033-continuidad-de-traza-en-saltos-asincronos.md) | Continuidad del contexto de traza en los saltos asíncronos | Aceptado | 2026-08-19 |

## Estados posibles

`Propuesto` · `Aceptado` · `Aceptado con riesgos residuales` · `Reemplazado por ADR-XXXX` · `Obsoleto`
