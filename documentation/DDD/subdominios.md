# Subdominios del LMS (DDD Estratégico)

- **Proyecto:** Learning Management System (LMS)
- **Versión:** 1.0
- **Fecha:** 2026-07-13
- **Estado:** Aceptado
- **Ámbito:** diseño estratégico
- **Decisión asociada:** [ADR-0003](../ADR/ADR-0003-clasificacion-de-subdominios.md)

---

## 1. Propósito

Clasificar las **capacidades de negocio** del LMS en subdominios **Core / Supporting / Generic**
para decidir dónde invertir el mejor esfuerzo de diseño y qué comprar o delegar.

> Principio rector: la clasificación es un juicio **estratégico** (espacio del problema),
> **independiente del nivel de madurez de la implementación actual**. Un Core sigue siendo
> Core aunque hoy esté implementado de forma simple.

## 2. Capacidades de negocio

| Capacidad | Conceptos del Lenguaje Ubicuo |
|---|---|
| Identidad y Acceso | Usuario, Rol, Administrador, Instructor, Estudiante |
| Autoría de Cursos | Curso *(editable)*, Lección, Borrador, Publicado, Publicar |
| Catálogo y Descubrimiento | Catálogo, Curso *(publicado/explorable)* |
| Matriculación | Matrícula, Tipo de matrícula (Free/Paid) |
| Aprendizaje y Progreso | Progreso, Completar lección, Elegibilidad, Finalización *(hecho)* |
| Certificación | Certificado, Emitir/verificar certificado |
| Administración | *(reutiliza conceptos de las demás para supervisar)* |
| Pagos | Precio, Cobro, Reembolso, Cupón |

## 3. Clasificación de subdominios

### Core Domain
| Subdominio | Justificación estratégica |
|---|---|
| **Aprendizaje y Progreso** | Es la razón de existir de la plataforma: la gente viene a *aprender*. Es donde el negocio vive o muere. Aquí va el mejor diseño (DDD táctico rico). |

### Supporting Domains
| Subdominio | Justificación |
|---|---|
| **Autoría de Cursos** | Necesaria y específica; no diferenciadora en el MVP. *(Candidata a Core si las herramientas del creador se vuelven el foso competitivo.)* |
| **Matriculación** | Puerta de entrada al valor; concentra la concesión de acceso, gratuita o pagada. |
| **Certificación** | Empaqueta el valor producido por Aprendizaje. *(Candidata a Core si la credibilidad/verificabilidad de la credencial se vuelve el diferenciador.)* |
| **Autorización específica del LMS** | Reglas propias del dominio: quién puede publicar, matricularse, etc. Transversal a los contextos. |
| **Orquestación de pagos** | La coordinación (precios, cupones, reembolsos, compensaciones) es lógica de negocio propia. |

### Generic Domains
| Subdominio | Justificación |
|---|---|
| **Autenticación** | Problema resuelto en todas partes: se adopta una solución existente (**Keycloak**). |
| **Procesamiento de pagos** | Cobrar es un problema resuelto: se delega en un proveedor externo. |

## 4. Lo que NO es subdominio

| Concepto | Decisión | Razón |
|---|---|---|
| **Catálogo** | Proyección derivada de **Autoría** | En el MVP no tiene reglas propias (ni ranking, ni recomendación, ni relevancia). Es una vista de los cursos publicados. |
| **Administración** | Actor/rol **transversal** | No tiene dominio propio; consume capacidades de otros contextos con permisos privilegiados. |

## 5. Evolución futura (registrada, no pendiente)

- **Catálogo → subdominio propio** si emergen reglas de descubrimiento (búsqueda, ranking, recomendación, personalización).
- **Certificación → Core** si el negocio apuesta por credenciales verificables como diferenciador.
- **Autoría → Core** si las herramientas del creador se vuelven ventaja competitiva.
- **Administración → subdominios nombrados** (Moderación, Gobernanza, Reportería) si aparecen conceptos de negocio propios.
- **Autorización LMS:** definir en el paso de Bounded Contexts cómo se manifiesta lo transversal.

## Historial de versiones

| Versión | Fecha | Cambios |
|---|---|---|
| 1.0 | 2026-07-13 | Clasificación inicial de subdominios aceptada. |
