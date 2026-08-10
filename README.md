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

## 9. Roadmap por incrementos

| # | Incremento | Estado |
|---:|---|---|
| 1 | Documentación, ADR y diagramas | **En curso** |
| 2 | Course Authoring | Pendiente |
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

## 10. Requisitos académicos

La trazabilidad completa de los criterios de los tres cursos está en
[academic-traceability.md](./docs/architecture/academic-traceability.md).
En esta fase el estado máximo posible es **Diseñado / Documentado / Pendiente**: no se marca ningún
criterio como implementado, probado ni demostrable.

## 11. Estado de implementación

| Aspecto | Estado |
|---|---|
| Documentación y ADR | **Documentado** |
| Diagramas iniciales | **Documentado** |
| Código de servicios | **No existe** |
| Contenedores y manifiestos | **No existen** |
| Pruebas | **No existen** |

## 12. Secciones pendientes de este README

Se completarán en incrementos posteriores: requisitos de ejecución · ejecución local con Docker
Compose · despliegue en Kubernetes · configuración de Keycloak · configuración del Gateway ·
configuración del broker · observabilidad · colección de pruebas · troubleshooting.
