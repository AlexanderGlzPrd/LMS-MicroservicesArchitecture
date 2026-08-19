# ADR-0026 — Apertura mínima de Enrollment: ConcederMatriculaPorPagoCapturado

## Estado
Aceptado con riesgos residuales — 2026-07-16

## Contexto
El diseño de aplicación de Enrollment establecía un único caso de escritura, `MatricularEstudiante`,
con **auto-matrícula** y `ActorId == StudentId`. La Saga de compra necesita conceder el acceso
**después de capturar el pago**, cuando el actor ya no es directamente el estudiante.

## Problema
¿Cómo permitir la concesión derivada de una compra sin habilitar la matrícula arbitraria de terceros
ni contaminar el dominio?

## Alternativas consideradas
- **Identidad delegada mediante intercambio de tokens**: preserva literalmente la regla del actor,
  pero la vigencia del token no sobrevive a una Saga que puede durar minutos, y exige un emisor de
  identidad delegada con su propio ciclo de vida.
- **Reutilizar `MatricularEstudiante` con credenciales de servicio**: **descartada** — el servicio
  podría enviar cualquier identificador de estudiante sin evidencia de intención.
- **Aserción firmada por el propio orquestador**: **descartada** — si el mismo servicio la genera y la
  firma, comprometerlo permite fabricarla; no prueba consentimiento.
- **Caso de aplicación interno específico, con evidencia en el estado persistido de la compra**.

## Decisión
Se añade a Enrollment un **segundo caso de aplicación de escritura**:
**`ConcederMatriculaPorPagoCapturado`**.

**Transporte:** **RabbitMQ**. **No existe endpoint HTTP**, **no transporta el JWT del estudiante**,
**no transporta token interactivo** y **no recibe importe ni información de pago**.
**Payload:** `PurchaseId`, `StudentId`, `CourseId`.

**Autorización:** usuario de broker exclusivo por servicio con **permisos mínimos**; **solo
`paid-enrollment` puede publicar** el comando y **Enrollment lo consume desde una cola dedicada**.
La **identidad técnica del servicio no sustituye la identidad del estudiante**.

**Consentimiento:** el `Purchase` fue creado desde una **solicitud autenticada**, con `StudentId`
tomado del claim `sub`; `StudentId` y `CourseId` quedan congelados en el `Purchase`, cuyo estado
persistido conserva la intención. **No existe aserción criptográfica autoemitida.**

**Ledger de Enrollment** (registro de aplicación, **separado del Aggregate Root `Matrícula`**):
clave `PurchaseId`, más `StudentId`, `CourseId`, resultado, origen, fecha y el identificador del
mensaje inicial.

**Garantías:** un `PurchaseId` no se reutiliza para otra pareja · un identificador de mensaje nuevo
con el mismo `PurchaseId` devuelve el resultado confirmado · el mismo identificador de mensaje se
deduplica por Inbox · **`EstudianteMatriculado` solo se produce cuando se crea la Matrícula** y
**nunca se reemite** en duplicados ni en `AlreadyExisted`.

**No se modifican:** el Aggregate Root `Matrícula`, sus invariantes (curso publicado y unicidad por
estudiante y curso) ni el evento producido.

## Justificación
Es la apertura más pequeña que satisface la Saga: explícita, auditable y separable del camino humano.
La evidencia de consentimiento es el registro de compra creado desde una petición autenticada, no un
artefacto que el propio servicio pueda fabricar.

## Consecuencias positivas
- Saga implementable sin tocar el dominio del LMS.
- Auditoría completa por `PurchaseId`.
- El camino gratuito permanece inalterado.

## Consecuencias negativas
- Enrollment pasa de uno a dos casos de escritura.
- Un registro adicional de aplicación que mantener y purgar.

## Riesgos residuales
- **Frontera de confianza:** si `paid-enrollment` fuera comprometido, podría emitir comandos válidos.
  Se mitiga por contención: permisos mínimos de broker, ausencia de ruta pública, ledger y auditoría.
  La solución de grado productivo sería un emisor independiente de identidad delegada.
- Un pago sobre acceso preexistente termina en revisión manual; la política comercial de reembolso
  queda fuera del alcance del MVP.

## Decisiones relacionadas
[ADR-0009](./ADR-0009-comunicacion-sincrona-y-asincrona.md) · [ADR-0011](./ADR-0011-outbox-transaccional.md) · [ADR-0012](./ADR-0012-inbox-y-deduplicacion.md) · [ADR-0016](./ADR-0016-saga-de-compra-de-acceso.md) · [ADR-0018](./ADR-0018-seguridad-con-keycloak.md)
