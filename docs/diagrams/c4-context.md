# C4 — Nivel 1: Contexto

**Propósito:** situar el sistema entre sus actores y sistemas externos.
**Audiencia:** evaluadores y personas no técnicas. **Criterio académico:** Curso 1 (arquitectura general).

```mermaid
flowchart TB
    INS["👤 Instructor<br/>crea y publica cursos"]
    STU["👤 Estudiante<br/>se matricula, aprende y se certifica"]
    ADM["👤 Administrador<br/>rol reservado en el MVP"]
    THIRD["👤 Tercero<br/>verifica un certificado"]

    LMS["🎓 LMS<br/>Sistema de gestión de aprendizaje<br/>(MVP + extensión académica de compra)"]

    KC["🔐 Keycloak<br/>Proveedor de identidad<br/>(subdominio Generic)"]
    PAY["💳 Proveedor de pago simulado<br/>(extensión académica)"]

    INS -->|crea, publica| LMS
    STU -->|explora, se matricula,<br/>marca lecciones, se certifica| LMS
    ADM -->|supervisa| LMS
    THIRD -->|verifica por CertificateId| LMS

    LMS -->|autenticación OAuth2/OIDC<br/>y consulta de nombre vía Admin API| KC
    LMS -->|autoriza, captura,<br/>anula, reembolsa| PAY
```

## Notas

- **Keycloak no es un Bounded Context de negocio**: es el proveedor de identidad (Generic).
- El **proveedor de pago es simulado** y pertenece **solo** a la extensión académica.
- La **verificación de certificados es pública**: no requiere autenticación, solo el `CertificateId`,
  y devuelve exclusivamente los datos congelados mínimos.
