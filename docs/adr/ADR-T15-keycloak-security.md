# ADR-T15 — Seguridad con Keycloak (OAuth2 / OIDC / JWT)

## Estado
Aceptado — 2026-07-16

## Contexto
Se requiere autenticación y autorización con OAuth2, OpenID Connect y JWT, con roles diferenciados.
Además, Certification necesita el **nombre visible** de un estudiante identificado por un hecho
asíncrono, sin disponer de un token interactivo de esa persona.

## Problema
¿Cómo se estructura la identidad, cómo se protege cada capa y de dónde se obtiene el nombre del
estudiante?

## Alternativas consideradas
Para el nombre del estudiante:
- **Endpoint `userinfo`**: **descartado** — representa al usuario del token presentado, no a un
  tercero identificado por un evento.
- **Servicio propio de perfiles**: un microservicio adicional para un solo atributo.
- **Proyección de nombres por eventos de identidad**: requiere extender el proveedor; frágil.
- **Incluir el nombre en `CursoFinalizado`**: violaría el contrato, ya que Learning no posee ese dato
  ni debe transportar información ajena.
- **Keycloak Admin API con cuenta de servicio y ACL mínimo**: usa al propietario legítimo del dato.

## Decisión
**Keycloak** como proveedor de identidad (subdominio *Generic*; **no** es un Bounded Context de
negocio). Realm `lms`, cliente confidencial para el Gateway, cliente público con PKCE para pruebas y
clientes de servicio para comunicación máquina-a-máquina. Roles: `Student`, `Instructor`,
`Administrator`.

**Seguridad en profundidad:** el Gateway valida el JWT y **cada microservicio lo vuelve a validar**
(firma, emisor, **audiencia propia**, expiración). Cada caso de uso aplica su propia autorización.

**Identidad del actor:** se deriva **siempre del claim `sub`**. Ningún identificador enviado en el
cuerpo de la petición sustituye al actor.

**Nombre del estudiante:** **Keycloak Admin API mediante cuenta de servicio y ACL mínimo**, con
permisos limitados a consultar usuarios; el ACL extrae solo el nombre visible. Certification no debe
recibir correo, roles, grupos, credenciales ni perfil completo.

## Justificación
Se usa el propietario legítimo del dato con el menor privilegio posible y sin añadir servicios. La
revalidación por servicio evita confiar únicamente en el borde.

## Consecuencias positivas
- Identidad centralizada y estándar, sin IAM propio.
- Autorización verificable por rol y por caso de uso.
- Verificación pública de certificados sin exponer datos personales innecesarios.

## Consecuencias negativas
- Dependencia operativa de Keycloak.
- Validación repetida del token en cada salto.

## Riesgos residuales
Indisponibilidad de Keycloak impide autenticar y también congelar el nombre al emitir; en ese caso no
se emite el certificado y se reintenta más tarde.

## Relación con criterios académicos
Curso 3: OAuth2, OpenID Connect, JWT, Keycloak con realm, clientes, roles y usuarios. Curso 2: JWT.

## Decisiones relacionadas
[T14](./ADR-T14-yarp-gateway.md) · [T19](./ADR-T19-resilience.md) · [T23](./ADR-T23-paid-enrollment-command.md)
