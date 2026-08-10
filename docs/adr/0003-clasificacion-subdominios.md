# ADR-0003: Clasificación de subdominios (Core/Supporting/Generic)

- **Estado:** Aceptado
- **Fecha:** 2026-07-13
- **Documento asociado:** [subdominios.md](../subdominios.md)

## 1. Contexto

DDD Estratégico. Antes de derivar Bounded Contexts, necesitamos saber **dónde invertir el
mejor esfuerzo de diseño** y **qué comprar/delegar**, clasificando las capacidades del LMS.

## 2. Problema

¿Qué capacidades son Core, Supporting o Generic? ¿Qué candidatos NO son realmente subdominios?

## 3. Alternativas consideradas

- **Clasificación inicial:** dos Cores (Aprendizaje + Certificación); Catálogo y Administración
  como subdominios; Identidad y Pagos como Generic "monolíticos".
- **Clasificación corregida (tras red-team):** un único Core; Certificación como Supporting;
  Catálogo como proyección; Administración como actor transversal; separar Autenticación
  (Generic) de Autorización LMS (Supporting), y Procesamiento de pago (Generic) de Orquestación
  (Supporting).

Errores detectados en la inicial: confundir **criticidad estratégica** con **madurez del MVP**,
e **inflación de "Core"**.

## 4. Decisión

- **Core:** Aprendizaje y Progreso.
- **Supporting:** Autoría de Cursos · Matriculación · Certificación · Autorización específica
  del LMS · Orquestación de pagos (Etapa 2).
- **Generic:** Autenticación · Procesamiento de pagos (Etapa 2).
- **Catálogo:** proyección derivada de Autoría (no subdominio en el MVP).
- **Administración:** actor transversal (no subdominio).

## 5. Justificación

La clasificación es estratégica e **independiente de la madurez de la implementación**
(un Core simple sigue siendo Core). Litmus aplicado: *"si un competidor hiciera esto mucho
mejor, ¿mataría el negocio?"*. Separar auth/authz y proceso/orquestación evita meter reglas
de negocio en herramientas genéricas (p. ej., Keycloak) o externalizar lógica propia.

## 6. Consecuencias

- El esfuerzo de diseño táctico rico se concentra en **Aprendizaje**.
- Lo **Generic se compra**: Keycloak (auth) y proveedor de pagos (procesamiento).
- Catálogo y Administración **no generan contextos/servicios propios** por sí mismos.
- **Decisiones abiertas** (registradas como evolución): Certificación y Autoría podrían ascender
  a Core si el negocio define ahí su diferenciador; la Autorización transversal debe ubicarse en
  el paso de Bounded Contexts.
