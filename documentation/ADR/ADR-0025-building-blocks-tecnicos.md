# ADR-0025 — Building blocks técnicos compartidos

## Estado
Aceptado — 2026-07-16

## Contexto
Varios servicios necesitan las mismas capacidades técnicas: sobre de mensaje, abstracciones de
Outbox e Inbox, correlación, logging, instrumentación, manejo uniforme de errores y comprobaciones
de salud. Duplicarlas en seis servicios es costoso; compartir dominio está prohibido.

## Problema
¿Qué puede compartirse entre servicios sin crear acoplamiento de dominio?

## Alternativas consideradas
- **No compartir nada**: duplicación e inconsistencias en aspectos puramente técnicos.
- **Una librería común que incluya dominio**: prohibido por el diseño estratégico.
- **Building blocks estrictamente técnicos**, sin ningún concepto de negocio.

## Decisión
Se permite compartir únicamente componentes técnicos:

- **Mensajería:** sobre de mensaje con sus metadatos y abstracciones de Outbox e Inbox.
- **Observabilidad:** correlación, logging estructurado e instrumentación.
- **Web:** middleware de errores, formato uniforme de respuesta de error y comprobaciones de salud.
- **Testing:** utilidades comunes de prueba.

**Queda prohibido** un proyecto de dominio compartido y, en general, compartir agregados, entidades,
Value Objects de negocio, estados, repositorios o cualquier clase de dominio.

## Justificación
Los aspectos transversales no pertenecen a ningún Bounded Context; compartirlos no acopla modelos.
El dominio, en cambio, es propiedad exclusiva de cada contexto.

## Consecuencias positivas
- Consistencia técnica y menor duplicación.
- Fronteras de dominio intactas.

## Consecuencias negativas
- Un cambio en un building block afecta a varios servicios: debe versionarse con cuidado.

## Riesgos residuales
Deriva hacia una librería “cajón de sastre”: se revisa que ningún concepto de negocio entre en ella.

## Decisiones relacionadas
[ADR-0005](./ADR-0005-monorepo.md) · [ADR-0006](./ADR-0006-clean-architecture-por-servicio.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md)
