# Arquitectura — Visión general

- **Proyecto:** LMS MVP (formación profesional y tecnológica)
- **Fecha:** 2026-07-16
- **Estado:** Diseño congelado (T01–T23). **No existe implementación todavía.**

---

## 1. Qué es este sistema

Plataforma de aprendizaje donde un **Instructor** crea y publica cursos, un **Estudiante** explora el
catálogo, se matricula, marca lecciones como completadas, finaliza el curso y obtiene un
**certificado de finalización** verificable públicamente.

## 2. Alcance: MVP funcional vs. extensión académica

El sistema tiene **dos ámbitos claramente separados**:

| Ámbito | Contenido | Naturaleza |
|---|---|---|
| **MVP funcional** | Course Authoring · Enrollment · Learning · Certification | flujo **gratuito**, es el LMS real |
| **Extensión académica** | paid-enrollment · payment-provider-sim | **compra de acceso**, existe para demostrar una **Saga con compensaciones** |

> El **flujo gratuito permanece independiente**: no depende de la extensión de compra, no cambia su
> comportamiento y sus contextos no la conocen. La única apertura es un segundo caso de escritura en
> Enrollment (ver [ADR-T23](../adr/ADR-T23-paid-enrollment-command.md)).

## 3. Servicios

| Servicio | Ámbito | Responsabilidad |
|---|---|---|
| `course-authoring` | MVP | crear/editar cursos y lecciones, publicar, republicar, catálogo |
| `enrollment` | MVP | conceder acceso de un estudiante a un curso |
| `learning` | **MVP — Core Domain** | progreso del estudiante y sellado de la Finalización |
| `certification` | MVP | emisión y verificación de certificados de finalización |
| `paid-enrollment` | académico | orquestador de la Saga de compra (estado persistido) |
| `payment-provider-sim` | académico | proveedor de pago simulado (autorizar/capturar/anular/reembolsar) |
| `gateway` (YARP) | técnico | punto de entrada único, routing y validación de JWT |
| `bff-composition` | técnico | API Composition (vistas compuestas) |

## 4. Comunicación en una frase

- **Síncrona (HTTP)** para **consultas de verificación** que exigen frescura.
- **Asíncrona (RabbitMQ)** para **hechos de negocio** con consumidor obligatorio y para los **pasos
  de la Saga que modifican estado**.

Detalle completo en [communication-matrix.md](./communication-matrix.md).

## 5. Principios arquitectónicos congelados

1. **Un Aggregate Root por transacción.** No existen transacciones distribuidas.
2. **Ningún hecho local se revierte** porque un servicio downstream falle.
3. **Fail-safe**: si una precondición externa no puede verificarse, la operación **no se ejecuta**.
4. **Entrega at-least-once** en mensajería; el **efecto de negocio es effectively-once** gracias a
   Inbox por `MessageId`, claves naturales y restricciones de unicidad.
5. **No se publica ningún evento sin consumidor.**
6. **Database per Service.** Sin tablas compartidas, sin joins entre servicios.
7. **Sin `Shared.Domain`.** Solo se comparten *building blocks* técnicos y contratos de mensaje.
8. El **flujo gratuito es coreografía EDA**, no una Saga. La Saga pertenece a la extensión de compra.

## 6. Riesgos aceptados (resumen)

| # | Riesgo | Tratamiento |
|---|---|---|
| 1 | Ventana entre concesión de Matrícula y creación de ProgresoDelCurso | consistencia eventual, reintento |
| 2 | Carrera comprobación→sellado en Learning | acotada a la misma operación; documentada |
| 3 | Emisión de certificado diferida si falta nombre o título | no se emite parcial; reintento |
| 4 | Marcar lecciones no disponible si Authoring cae | **decisión consciente** a favor de la integridad del Core |
| 5 | Frontera de confianza en `paid-enrollment` | permisos mínimos de broker, ledger y auditoría |
| 6 | `ManualReview` exige intervención humana | por diseño: no se falsea una compensación |

## 7. Documentos relacionados

- [Bounded Contexts](./bounded-contexts.md) · [Flujos de aplicación](./application-flows.md)
- [Arquitectura técnica](./technical-architecture.md) · [Matriz de comunicación](./communication-matrix.md)
- [Fiabilidad e idempotencia](./reliability-and-idempotency.md) · [Seguridad](./security-architecture.md)
- [Trazabilidad académica](./academic-traceability.md)
- Estratégicos previos: [Lenguaje Ubicuo](../lenguaje-ubicuo.md) · [Subdominios](../subdominios.md)
- [Índice de ADR](../adr/README.md)
