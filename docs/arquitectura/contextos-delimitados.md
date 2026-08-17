# Bounded Contexts

- **Fecha:** 2026-07-16 · **Estado:** congelado
- Base estratégica: [subdominios.md](../subdominios.md) · [lenguaje-ubicuo.md](../lenguaje-ubicuo.md) · [ADR-0003](../adr/0003-clasificacion-subdominios.md)

---

## 1. Mapa

| Bounded Context | Nivel estratégico | Aggregate Root | Identidad |
|---|---|---|---|
| **Course Authoring** | Supporting | `Curso` (entidad interna `Lección`) | `CourseId` |
| **Enrollment** | Supporting | `Matrícula` | `(StudentId, CourseId)` |
| **Learning** | **Core Domain** | `ProgresoDelCurso` | `(StudentId, CourseId)` |
| **Certification** | Supporting | `Certificado` | `CertificateId` |

**Fuera del mapa de dominio:** Autenticación = subdominio *Generic* (Keycloak, proveedor de
identidad, **no** es un Bounded Context de negocio) · Catálogo = **proyección** de Authoring ·
Administración = actor transversal.

## 2. Course Authoring

Custodia la creación, estructura y publicación del contenido.

- **Invariantes:** un Curso nace en Borrador · requiere **≥1 Lección** para publicarse · solo el
  instructor propietario lo modifica · `Lección` tiene identidad estable.
- **Contenido de trabajo vs. contenido publicado** son conceptos distintos; los cambios posteriores
  a la primera publicación **solo llegan a downstream al republicar**. **No hay historial de versiones.**
- **No pertenecen:** matrícula, progreso, finalización, certificado.
- **Eventos de dominio:** `CursoPublicado`, `ContenidoPublicadoModificado`. **No se publican al
  broker**: no tienen consumidor obligatorio (ver [ADR-T12](../adr/ADR-T12-conjunto-de-lecciones-vigente.md)).

## 3. Enrollment

Gobierna la concesión de acceso. La **existencia de la Matrícula significa acceso concedido**.

- **Invariantes:** una Matrícula por `(StudentId, CourseId)` · solo en curso publicado.
- **Estado único** en el MVP: no hay cancelación, expiración ni desmatriculación; **no existe
  “matrícula completada”**.
- **No pertenecen:** progreso, completado, certificado, contenido del curso, datos de pago.
- **Integration Event:** `EstudianteMatriculado` (`StudentId`, `CourseId`) → consumido por Learning.

## 4. Learning — Core Domain

Custodia el progreso del estudiante y determina la Finalización.

- **Estado:** `StudentId`, `CourseId`, `LessonIds` completadas, `EnProgreso | Finalizado`,
  `Finalización` opcional e **inmutable**.
- **Reglas:** el progreso nace al reconocer el acceso · completar es una **acción manual** ·
  solo se completa una `LessonId` **actual** · el agregado calcula el 100% y **sella** ·
  `ListoParaFinalizar` es **condición derivada, no estado persistido** · marcar la última lección
  puede finalizar · eliminar una lección **no finaliza automáticamente** (para eso existe
  `ConfirmarFinalizacion`) · `CursoFinalizado` se produce **una sola vez**.
- **El conjunto actual de `LessonIds` pertenece a Authoring** y **no forma parte del estado del
  agregado**: se obtiene **fresco en toda escritura** (ver [ADR-T12](../adr/ADR-T12-conjunto-de-lecciones-vigente.md)).
- **No pertenecen:** contenido editorial, matrícula completa, `EnrollmentType`, pagos, certificado.
- **Integration Event:** `CursoFinalizado` (`StudentId`, `CourseId`, fecha) → consumido por Certification.

## 5. Certification

Emite y verifica el **certificado de finalización** (no acredita competencia ni aprobación).

- **Estado:** `CertificateId` · referencia a la Finalización · `StudentSnapshot` · `CourseSnapshot` ·
  fecha de Finalización · emisor = **la plataforma**.
- **Reglas:** se emite solo desde una Finalización válida · **uno por Finalización** · nace **solo con
  información completa** · nombre y título se **congelan al emitir** · la fecha procede de la
  Finalización · **inmutable** · **verificación pública por `CertificateId`, sin mutar estado** ·
  no hay revocación en el MVP.
- **No pertenecen:** progreso, elegibilidad, detalle de lecciones, matrícula, pagos, ciclo editorial.
- **Evento de dominio:** `CertificadoEmitido` — **no se publica** (sin consumidor).

## 6. Extensión opcional (fuera del dominio del LMS)

| Componente | Rol |
|---|---|
| `paid-enrollment` | orquestador de la Saga; posee el agregado `Purchase` y su máquina de estados |
| `payment-provider-sim` | proveedor de pago simulado (autorizar, capturar, anular, reembolsar) |

Estos componentes **no forman parte del dominio del LMS** y **ningún BC del MVP depende de ellos**.
