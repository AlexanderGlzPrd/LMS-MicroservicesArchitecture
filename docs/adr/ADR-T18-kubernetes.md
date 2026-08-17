# ADR-T18 — Despliegue en Kubernetes

## Estado
Aceptado — 2026-07-16

## Contexto
La solución debe ser cloud-native y desplegarse en Kubernetes, con evidencia de escalabilidad y de
recuperación ante fallos.

## Problema
¿Cómo se clasifican y despliegan los componentes, y cómo se demuestra escalado y resiliencia?

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
| Jaeger | **Deployment** | almacenamiento volátil en el entorno académico |

Además: ConfigMaps para configuración, Secrets para credenciales, **readiness y liveness probes** en
todos los servicios, límites de recursos y **escalado automático en `learning`**.

**Prueba de escalabilidad:** escalar `learning` a tres réplicas, generar carga sobre sus consultas y
observar el reparto y la disponibilidad.
**Prueba de resiliencia:** eliminar un pod, verificar su recreación, comprobar la continuidad del
flujo y revisar logs, métricas y trazas.

> **Declaración obligatoria:** desplegar bases de datos y broker **dentro del clúster** es una
> decisión **académica y local**, no una recomendación de producción. En un entorno productivo serían
> **servicios administrados**.

## Justificación
La clasificación por naturaleza evita objetos mal elegidos y permite demostrar escalado y
recuperación con componentes realmente sin estado.

## Consecuencias positivas
- Escalado y recuperación demostrables.
- Configuración y secretos externalizados.

## Consecuencias negativas
- Complejidad operativa notable.
- Consumo de recursos elevado para un clúster local.

## Riesgos residuales
Pérdida de trazas al reiniciar Jaeger en el entorno académico; las evidencias deben capturarse
durante la demostración.

## Relación con criterios académicos
Curso 3: cloud-native, Deployments, Services, Pods, réplicas, escalabilidad, resiliencia,
troubleshooting, manifiestos y configuración.

## Decisiones relacionadas
[T04](./ADR-T04-base-de-datos-por-servicio.md) · [T16](./ADR-T16-observabilidad.md) · [T17](./ADR-T17-docker-compose.md)
