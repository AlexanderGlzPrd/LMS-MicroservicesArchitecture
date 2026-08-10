# ADR-T16 — Observabilidad

## Estado
Aceptado — 2026-07-16

## Contexto
Un flujo distribuido con mensajería debe poder observarse de extremo a extremo: qué ocurrió, en qué
servicio, en qué orden y por qué falló. Las rúbricas exigen métricas, trazas distribuidas y logs.

## Problema
¿Cómo se instrumentan los servicios y qué debe poder demostrarse?

## Alternativas consideradas
- **Solo logs**: insuficiente para seguir un flujo entre servicios.
- **Instrumentación propietaria por herramienta**: acopla el código a un proveedor.
- **OpenTelemetry como estándar**, con exportación a herramientas especializadas.

## Decisión
**OpenTelemetry** como instrumentación única, con **Jaeger** para trazas, **Prometheus** para
métricas, **Grafana** para dashboards y **logging estructurado**.

**Correlación obligatoria** propagada tanto en HTTP como en mensajería: identificador de traza,
identificador de correlación, identificador de causalidad, identificador de mensaje e identificador
de compra en la Saga.

**Métricas técnicas:** latencia, tasa de error, profundidad de cola, mensajes en cola de mensajes
muertos, estado del Circuit Breaker, reintentos.
**Métricas de negocio:** matrículas, lecciones completadas, finalizaciones, certificados emitidos,
**estados de Saga y compensaciones ejecutadas**.

**Flujos que deben poder trazarse completos:** matrícula → creación de progreso · última lección →
sellado → emisión de certificado · cada estado y compensación de la Saga.

## Justificación
Un estándar único evita reinstrumentar por herramienta y permite correlacionar los tres tipos de
señal alrededor de un mismo flujo.

## Consecuencias positivas
- Diagnóstico real de fallos distribuidos.
- Evidencia académica de alto valor: traza completa, dashboards y logs correlacionados.

## Consecuencias negativas
- Trabajo de instrumentación y propagación de contexto en la mensajería.
- Componentes adicionales que desplegar.

## Riesgos residuales
En el entorno académico el almacenamiento de trazas puede ser volátil; las evidencias deben
capturarse durante la demostración.

## Relación con criterios académicos
Curso 3: Prometheus, Grafana, Jaeger, métricas, logs, trazabilidad distribuida, troubleshooting.
Curso 2: logging estructurado.

## Decisiones relacionadas
[T07](./ADR-T07-rabbitmq-masstransit.md) · [T18](./ADR-T18-kubernetes.md) · [T19](./ADR-T19-resilience.md)
