# Flujos de aplicación (casos de uso)

- **Fecha:** 2026-07-16 · **Estado:** congelado
- Clasificación: **comando humano** · **proceso iniciado por hecho** · **consulta**

---

## 1. Course Authoring

| Caso de uso | Tipo | Actor | Resultado |
|---|---|---|---|
| CrearCurso | comando | Instructor | curso en Borrador |
| AgregarLeccion / EditarLeccion / EliminarLeccion / ReordenarLecciones | comando | Instructor propietario | contenido de trabajo actualizado |
| PublicarCurso | comando | Instructor propietario | Borrador→Publicado (evento de dominio) |
| RepublicarCurso | comando | Instructor propietario | contenido publicado reemplazado, **o no-op sin cambios** |
| ExplorarCatalogo | consulta **pública** | Estudiante/visitante | resúmenes publicados |
| ConsultarContenidoPublicado | consulta | público/estudiante | contenido publicado |
| ConsultarResumenCurso · ListarCursosDelInstructor | consulta | Instructor | vistas propias |
| ConsultarCursoParaEdicion | consulta | Instructor propietario | **contenido de trabajo** |

> `EditarCursoPublicado` **no es un caso separado**: se usan los comandos normales, y el agregado
> decide si modifica el contenido de trabajo o el publicado. **El contenido de trabajo nunca cruza
> a downstream.**

## 2. Enrollment

| Caso de uso | Tipo | Iniciador | Resultado |
|---|---|---|---|
| MatricularEstudiante | comando | **Estudiante (sí mismo)**, `ActorId == StudentId` | acceso concedido · `EstudianteMatriculado` |
| **ConcederMatriculaPorPagoCapturado** | **comando interno por mensaje** | `paid-enrollment` | acceso concedido · `EstudianteMatriculado` **solo si se crea** |
| ConsultarAcceso | consulta | Estudiante / paid-enrollment | acceso sí/no |
| ListarCursosMatriculadosDelEstudiante | consulta | Estudiante | `CourseId`s |

Ambos comandos comparten **las mismas invariantes y el mismo evento**. Diferencias en
[ADR-T23](../adr/ADR-T23-paid-enrollment-command.md).

## 3. Learning (Core)

| Caso de uso | Tipo | Iniciador | Información externa |
|---|---|---|---|
| ReconocerAccesoConcedido | **proceso por hecho** | `EstudianteMatriculado` | — |
| MarcarLeccionComoCompletada | comando | Estudiante | **conjunto fresco de `LessonIds`** |
| ConfirmarFinalizacion | comando | Estudiante | **conjunto fresco de `LessonIds`** |
| ConsultarProgresoDelCurso | consulta | Estudiante | caché de lectura (**% aproximado**) |
| ListarCursosEnProgreso · ListarCursosFinalizados | consulta | Estudiante | — |

**Ninguna consulta finaliza el curso.** El porcentaje es **vista de lectura**, no estado.

## 4. Certification

| Caso de uso | Tipo | Iniciador | Resultado |
|---|---|---|---|
| EmitirCertificadoPorFinalización | **proceso por hecho** | `CursoFinalizado` | certificado emitido, o **no-op idempotente** |
| ConsultarCertificado | consulta | Estudiante propietario | datos congelados |
| **VerificarCertificado** | consulta **pública** | tercero (con `CertificateId`) | válido/no válido + mínimos congelados |
| ListarCertificadosDelEstudiante | consulta | Estudiante propietario | credenciales |

## 5. Extensión académica — paid-enrollment

| Caso de uso | Tipo | Iniciador |
|---|---|---|
| IniciarCompraDeAcceso | comando | Estudiante (autenticado; `StudentId` del claim `sub`) |
| ConsultarEstadoDeCompra | consulta | Estudiante propietario |
| Resoluciones operativas de `ManualReview` | operativo | operador |

## 6. Flujo extremo a extremo (gratuito)

```
Instructor: crear → agregar lecciones → publicar
Estudiante: explorar catálogo → matricularse
  → [EstudianteMatriculado] → Learning crea ProgresoDelCurso
Estudiante: marcar lecciones → (la última puede sellar la Finalización)
  → [CursoFinalizado] → Certification reúne snapshots → emite Certificado
Estudiante: consultar Certificado · Tercero: verificarlo públicamente
```

Este flujo es **coreografía EDA con consumidores idempotentes**, **no una Saga**: sus hechos son
irreversibles y no admiten compensación legítima.

## 7. Composición de lectura

`GET /me/courses-in-progress` (BFF): Learning aporta `CourseId`, estado y %; Authoring aporta título
y nº de lecciones. Respuesta degradada: **200 con `isPartial: true`** y `warnings[]` si Authoring no
responde; **503** si Learning no responde.
