# ADR-0021 — Despliegue en Kubernetes

## Estado
Aceptado — 2026-07-16

## Contexto
La solución se despliega en Kubernetes, y debe escalar y recuperarse de la caída de un pod sin
intervención manual.

## Problema
¿Cómo se clasifican y despliegan los componentes, y cómo se verifican escalado y recuperación?

## Alternativas consideradas
- **Todo como Deployment**: incorrecto para componentes con estado persistente.
- **Todo como StatefulSet**: innecesario para servicios sin estado.
- **Clasificación por naturaleza del componente**: cada uno según si conserva estado.

## Decisión

| Componente | Objeto | Motivo |
|---|---|---|
| Servicios de aplicación, Gateway, BFF | **Deployment** | sin estado |
| **Keycloak** | **Deployment** | su estado vive en la base de datos externa |
| PostgreSQL (uno por servicio) | **StatefulSet con volumen** | estado persistente |
| RabbitMQ | **StatefulSet con volumen** | estado persistente |
| Prometheus | **StatefulSet con volumen** | almacena series temporales |
| Grafana | **Deployment** | dashboards como configuración |
| Jaeger | **Deployment** | almacenamiento en memoria, sin persistencia |

Además: ConfigMaps para configuración, Secrets para credenciales, **readiness y liveness probes** en
todos los servicios, límites de recursos y **escalado automático en `learning`**.

**Verificación de escalabilidad:** escalar `learning` a tres réplicas, generar carga sobre sus
consultas y observar el reparto y la disponibilidad.
**Verificación de resiliencia:** eliminar un pod, verificar su recreación, comprobar la continuidad
del flujo y revisar logs, métricas y trazas.

> **Advertencia:** desplegar bases de datos y broker **dentro del clúster** es una decisión válida
> para este entorno, **no una recomendación de producción**. En producción serían **servicios
> administrados**.

## Justificación
La clasificación por naturaleza evita objetos mal elegidos y concentra el escalado en los
componentes que realmente no tienen estado.

## Consecuencias positivas
- Escalado y recuperación automáticos y verificables.
- Configuración y secretos externalizados.

## Consecuencias negativas
- Complejidad operativa notable.
- Consumo de recursos elevado para un clúster local.

## Riesgos residuales
Reiniciar Jaeger pierde el histórico de trazas. Si la retención llega a hacer falta, exige un
backend persistente.

## Decisiones relacionadas
[ADR-0007](./ADR-0007-base-de-datos-por-servicio.md) · [ADR-0019](./ADR-0019-observabilidad.md) · [ADR-0020](./ADR-0020-docker-y-docker-compose.md)
