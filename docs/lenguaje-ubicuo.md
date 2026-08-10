# Lenguaje Ubicuo (Ubiquitous Language) — LMS

- **Proyecto:** Learning Management System (LMS) — plataforma de formación profesional/tecnológica.
- **Versión:** 1.1
- **Fecha:** 2026-07-12
- **Estado:** Aceptado
- **Etapa:** Etapa 1 (MVP · Microservicios · DDD · Clean Architecture)

---

## 1. Propósito de este documento

Definir un vocabulario **compartido, preciso y sin ambigüedad** entre el negocio y el
código. Todo término aquí definido debe usarse *tal cual* en conversaciones, diagramas,
nombres de clases, tablas de base de datos y endpoints de la API.

> Regla del proyecto: si un concepto no está en este documento, **no existe** en el
> sistema hasta que lo definamos aquí primero.

## 2. Convenciones

| Concepto | Convención |
|---|---|
| Idioma del negocio | Español (este documento) |
| Idioma del código | Inglés (nombre canónico entre paréntesis) |
| Regla de oro | Un término = un significado **dentro de su contexto** |

Cada término lleva su **nombre canónico en código** para evitar el problema de "cuatro
nombres para la misma cosa" (negocio, backend, base de datos, frontend).

---

## 3. Identidad y roles

> Consecuencia de la decisión: *una misma persona puede tener varios roles a la vez*
> (p. ej. ser Instructor y Estudiante simultáneamente).

| Término | Código | Significado |
|---|---|---|
| **Usuario** | `User` | Persona con una cuenta y credenciales. Es la *identidad*. |
| **Rol** | `Role` | Capacidad que un Usuario ejerce. Un Usuario puede tener **uno o varios** roles. |
| **Administrador** | `Administrator` | Rol que gestiona la plataforma y supervisa usuarios y cursos. |
| **Instructor** | `Instructor` | Rol que crea, edita y publica cursos. Es el *autor* del contenido. |
| **Estudiante** | `Student` | Rol que se matricula en cursos, consume el contenido y obtiene certificados. |

- Cuando un Usuario actúa **como Instructor**, crea/publica cursos.
- Cuando un Usuario actúa **como Estudiante**, se matricula y aprende.
- La gestión de identidad y roles se anticipa como candidata a un **Bounded Context**
  propio (Identidad y Acceso), alineado con la futura integración de **Keycloak** (Etapa 3).

---

## 4. Contenido y su estructura

| Término | Código | Significado |
|---|---|---|
| **Curso** | `Course` | Unidad de aprendizaje que un Instructor crea y publica. En el MVP se compone directamente de Lecciones (sin Módulos). |
| **Lección** | `Lesson` | Unidad mínima de contenido consumible dentro de un Curso. En el MVP contiene: **Título**, **Descripción** y **URL del video**. |
| **Catálogo** | `Catalog` | Colección de cursos **publicados** que un Estudiante puede explorar. |

---

## 5. Ciclo de vida del curso (lado Instructor)

| Término | Código | Significado |
|---|---|---|
| **Borrador** | `Draft` | Estado inicial de un Curso; **no** es visible para estudiantes. Un Curso se crea en este estado y **puede permanecer vacío** mientras el Instructor lo prepara. |
| **Publicado** | `Published` | Estado de un Curso visible en el Catálogo. |
| **Publicar** | `Publish` | Acción del Instructor que lleva un Curso de `Draft` a `Published`. En el MVP es directa (sin aprobación del Administrador). |

```
[ Draft ] ──Publicar──▶ [ Published ]
```

---

## 6. Ciclo del estudiante

| Término | Código | Significado |
|---|---|---|
| **Matrícula** | `Enrollment` | Vínculo entre un Estudiante y un Curso; le concede acceso al contenido. |
| **Tipo de matrícula** | `EnrollmentType` | `Gratuita` (`Free`) o `De pago` (`Paid`). En el MVP **siempre** es `Free`. |
| **Progreso** | `Progress` | Registro de qué Lecciones ha completado el Estudiante en un Curso. |
| **Completar lección** | `CompleteLesson` | Acción del Estudiante que marca una Lección como terminada. |
| **Curso completado** | `Completed` | Estado que alcanza la Matrícula cuando el Estudiante completa el **100 %** de las Lecciones del Curso. |

---

## 7. Certificación

| Término | Código | Significado |
|---|---|---|
| **Certificado** | `Certificate` | Documento que acredita que un Estudiante completó un Curso. |
| **Emitir certificado** | `IssueCertificate` | Acción de generar el Certificado cuando el Curso se completa. Se emite **una sola vez** por Matrícula completada. |

---

## 8. Invariantes clave del dominio

Reglas que el sistema debe garantizar siempre:

1. Un Curso puede **crearse y existir vacío** (sin Lecciones) mientras permanece en `Draft`.
2. Un Curso solo puede transicionar a `Published` si tiene **al menos una Lección**.
   Es una **invariante de negocio** (no una simple validación de API): vive en el modelo
   y protege la promesa de que todo Curso del Catálogo ofrece valor desde el primer momento.
3. Un Estudiante solo puede matricularse en un Curso que esté **`Published`**.
4. Un Curso se considera **`Completed`** para un Estudiante cuando ha completado el **100 %** de sus Lecciones.
5. El **Certificado** se emite exactamente **una vez**, en el momento en que el Curso se completa.

---

## 9. Eventos del dominio candidatos

Hechos relevantes del negocio (serán la base de la Event-Driven Architecture en la Etapa 2):

| Evento | Se produce cuando… |
|---|---|
| `CoursePublished` | Un Instructor publica un Curso. |
| `StudentEnrolled` | Un Estudiante se matricula en un Curso. |
| `LessonCompleted` | Un Estudiante completa una Lección. |
| `CourseCompleted` | Un Estudiante completa el 100 % de las Lecciones de un Curso. |
| `CertificateIssued` | Se emite el Certificado tras completar el Curso. |

> `CourseCompleted` → `CertificateIssued` es el primer flujo reactivo natural del
> dominio, y será el candidato ideal para introducir la comunicación por eventos.

---

## 10. Decisiones y evoluciones futuras (fuera del alcance del MVP)

| Tema | MVP (Etapa 1) | Evolución prevista |
|---|---|---|
| Estructura del Curso | Curso → Lecciones (plano) | Curso → **Módulos** → Lecciones |
| Contenido de Lección | Título + Descripción + URL de video | Materiales descargables, quizzes, evaluaciones |
| Regla de completado | 100 % de Lecciones | + Examen final |
| Pagos | Todos los cursos gratuitos (flujo directo) | **Payment** como Bounded Context nuevo (Etapa 2, con SAGA) |
| Publicación | El Instructor publica directamente | Aprobación previa del Administrador (`Draft → EnRevisión → Published`) |
| Identidad | Roles gestionados por el sistema | **Keycloak** / OAuth2 / JWT (Etapa 3) |

---

## Historial de versiones

| Versión | Fecha | Cambios |
|---|---|---|
| 1.0 | 2026-07-12 | Versión inicial acordada del Lenguaje Ubicuo del MVP. |
| 1.1 | 2026-07-12 | Confirmada la invariante de publicación (Curso vacío permitido en `Draft`; ≥1 Lección para `Published`). |
