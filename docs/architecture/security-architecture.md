# Arquitectura de seguridad

- **Fecha:** 2026-07-16 · **Estado:** congelado
- Decisiones: [ADR-T14](../adr/ADR-T14-yarp-gateway.md) · [ADR-T15](../adr/ADR-T15-keycloak-security.md) · [ADR-T23](../adr/ADR-T23-paid-enrollment-command.md)

---

## 1. Proveedor de identidad

**Keycloak** (OAuth2 / OpenID Connect / JWT). Es el subdominio *Generic* de autenticación: **no es un
Bounded Context de negocio** y **no se construye un IAM propio**.

- **Realm:** `lms`
- **Clientes:** `lms-gateway` (confidencial) · `lms-spa`/Postman (público, PKCE) · clientes de
  servicio para comunicación máquina-a-máquina.
- **Roles de realm:** `Student`, `Instructor`, `Administrator`.
- **Usuarios de prueba:** 1 instructor, 2 estudiantes, 1 administrador.

## 2. Seguridad en profundidad

| Capa | Responsabilidad |
|---|---|
| **Gateway (YARP)** | valida firma, `iss`, `aud`, `exp`; rechaza temprano; propaga el token; añade correlación; rate limiting. **Sin lógica de dominio** |
| **Cada microservicio** | **vuelve a validar** el JWT (firma, issuer, **audiencia propia**, expiración). Nunca se confía solo en el Gateway |
| **Cada caso de uso** | autorización propia por rol; **`ActorId` se deriva siempre del claim `sub`** |
| **BFF** | valida el JWT y **propaga el token del usuario** (no una identidad de servicio) |
| **Servicio ↔ servicio (HTTP)** | client-credentials con **audiencia específica del destino** |
| **Broker** | usuario propio por servicio y **permisos mínimos por exchange y cola** |

## 3. Regla de identidad

> **Ningún `StudentId` o `InstructorId` enviado en el cuerpo de la petición sustituye al actor.**
> Se ignora siempre y se usa el claim `sub`. Los endpoints emplean rutas `/me/**` o derivan la
> identidad del token.

## 4. Endpoints por nivel de acceso

| Nivel | Rutas |
|---|---|
| **Público (sin token)** | catálogo · contenido publicado de un curso · **verificación de certificado por `CertificateId`** |
| **Student** | matricularse · marcar lección · confirmar finalización · progreso · sus certificados · iniciar compra |
| **Instructor** | crear/editar/publicar/republicar sus cursos · sus cursos |
| **Administrator** | reservado (sin capacidades definidas en el MVP) |

La **verificación pública** devuelve únicamente lo mínimo congelado (validez, nombre, título, fecha,
emisor). **Nunca** expone el perfil del estudiante ni otras credenciales.

## 5. Nombre del estudiante para el certificado

Certification obtiene el nombre mediante **Keycloak Admin API con una cuenta de servicio y un ACL
mínimo**, con permisos limitados a la consulta de usuarios. El ACL extrae **solo el nombre visible**.

> **No se utiliza `userinfo`**: ese endpoint representa al usuario del token presentado, y
> Certification actúa a partir de un hecho asíncrono sin token interactivo de ese estudiante.

Certification **no debe recibir** correo, roles, grupos, credenciales ni perfil completo.

## 6. Seguridad de la Saga (extensión académica)

- El comando `ConcederMatriculaPorPagoCapturado` viaja **por RabbitMQ**. **No existe endpoint HTTP**,
  **no transporta JWT del estudiante** y **no transporta token interactivo**.
- La **autorización técnica es del broker**: solo el usuario `paid-enrollment` puede publicar en el
  exchange de comandos de Saga; `enrollment` solo puede leer de su cola dedicada.
- **El consentimiento del estudiante lo evidencia el `Purchase` persistido**, creado desde una
  solicitud autenticada con `StudentId` tomado del claim `sub` y congelado junto al `CourseId`.
- **No existe aserción criptográfica autoemitida**: un artefacto firmado por el propio servicio no
  probaría consentimiento frente a su compromiso.
- **La identidad técnica del servicio no sustituye la identidad del estudiante.**

### Frontera de confianza declarada

Si `paid-enrollment` fuera comprometido, podría emitir comandos de concesión válidos. Se mitiga por
**contención**: permisos mínimos de broker, ausencia de ruta pública, ledger por `PurchaseId`,
auditoría y alertas ante concesiones sin compra pagada. La solución de grado productivo sería un
**emisor independiente de identidad delegada**; queda documentada como evolución, no implementada.
