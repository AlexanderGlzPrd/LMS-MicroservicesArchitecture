# ADR-0009 — Comunicación síncrona y asíncrona

## Estado
Aceptado — 2026-07-16

## Contexto
Algunas reglas exigen información fresca en el instante de decidir; otras describen hechos ya
ocurridos que otros contextos deben conocer sin bloquear al productor.

## Problema
¿Qué relaciones se resuelven con llamadas síncronas y cuáles con mensajería?

## Alternativas consideradas
- **Todo síncrono**: consistencia inmediata, pero acopla la disponibilidad de cada servicio a la de
  todos aquellos a los que llama, incluso para propagar hechos ya ocurridos.
- **Todo asíncrono**: máximo desacople, pero impide verificar precondiciones que exigen frescura
  (curso publicado, conjunto actual de lecciones).
- **Mixto por naturaleza de la interacción**: síncrono para consultas de verificación, asíncrono para
  hechos de negocio y pasos de Saga que modifican estado.

## Decisión
Comunicación **mixta por naturaleza**:

- **HTTP síncrono** — Enrollment consulta a Authoring si el curso está publicado · Learning obtiene de
  Authoring el conjunto actual de `LessonIds` **en toda escritura** · Certification consulta el título
  y, mediante ACL sobre Keycloak Admin API, el nombre · el BFF consulta Learning y Authoring ·
  `paid-enrollment` usa HTTP para `ConsultarAcceso`.
- **RabbitMQ asíncrono** — `EstudianteMatriculado` (Enrollment → Learning) · `CursoFinalizado`
  (Learning → Certification) · los pasos de la Saga que modifican estado
  (`AutorizarPago`, `CapturarPago`, `AnularAutorizacion`, `ReembolsarPago`,
  `ConcederMatriculaPorPagoCapturado`) y sus respuestas.

**No se publican** `CursoPublicado`, `ContenidoPublicadoModificado`, `LecciónCompletada`,
`CertificadoEmitido`, `PurchaseConfirmada` ni `PurchaseCompensada`: no tienen consumidor obligatorio.

## Justificación
Cada mecanismo se aplica donde su garantía es necesaria. No se publica ningún mensaje sin consumidor:
la observabilidad se cubre con logs, métricas y trazas, que no requieren Integration Events.

## Consecuencias positivas
- Reglas críticas verificadas con datos frescos.
- Hechos propagados sin bloquear al productor.
- Superficie de mensajería mínima y justificada.

## Consecuencias negativas
- La disponibilidad de Authoring condiciona matricular, marcar lecciones y emitir certificados.
- Consistencia eventual entre servicios.

## Riesgos residuales
Ventana entre la concesión de acceso y la creación del progreso; caída de Authoring que bloquea
escrituras de Learning (decisión consciente a favor de la integridad del Core).

## Decisiones relacionadas
[ADR-0010](./ADR-0010-rabbitmq-y-masstransit.md) · [ADR-0015](./ADR-0015-conjunto-vigente-de-lecciones.md) · [ADR-0016](./ADR-0016-saga-de-compra-de-acceso.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md)
