# ADR-0019 — Observabilidad

## Estado
Aceptado — 2026-07-16

## Contexto
Un flujo distribuido con mensajería debe poder observarse de extremo a extremo: qué ocurrió, en qué
servicio, en qué orden y por qué falló. Con ocho componentes y un broker de por medio, el diagnóstico
por logs sueltos deja de ser viable.

## Problema
¿Cómo se instrumentan los servicios y qué debe poder seguirse de extremo a extremo?

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
- Traza completa, dashboards y logs correlacionados alrededor del mismo flujo.

## Consecuencias negativas
- Trabajo de instrumentación y propagación de contexto en la mensajería.
- Componentes adicionales que desplegar.

## Riesgos residuales
El almacenamiento de trazas es volátil en los entornos actuales: al reiniciar Jaeger se pierde el
histórico. Un backend persistente queda pendiente si la retención llega a hacer falta.

## Decisiones relacionadas
[ADR-0010](./ADR-0010-rabbitmq-y-masstransit.md) · [ADR-0021](./ADR-0021-despliegue-en-kubernetes.md) · [ADR-0022](./ADR-0022-politicas-de-resiliencia.md)
