# ADR-T19 — Políticas de resiliencia

## Estado
Aceptado — 2026-07-16

## Contexto
El sistema depende de llamadas síncronas entre servicios, de un broker y de un proveedor de
identidad. Cada dependencia puede fallar de forma transitoria o permanente.

## Problema
¿Qué política se aplica a cada tipo de fallo, sin comprometer la integridad del dominio?

## Alternativas consideradas
- **Reintentar todo indefinidamente**: agrava la saturación y enmascara errores funcionales.
- **Fallar de inmediato siempre**: penaliza fallos transitorios recuperables.
- **Política diferenciada por tipo de fallo y de operación**.

## Decisión

| Situación | Política |
|---|---|
| Llamadas síncronas | timeout acotado, reintentos con retroceso exponencial, **Circuit Breaker** |
| Precondición externa no verificable | **fail-safe**: la operación no se ejecuta |
| Consumo de mensajes | reintentos con retroceso y, agotados, **cola de mensajes muertos** |
| Errores funcionales (contrato o versión desconocidos, payload inválido) | **cola de mensajes muertos directa, sin reintentar**, con alerta |
| Mensajes *poison* | descarte tras el límite y alerta |
| Composición en el BFF | timeout por dependencia, Circuit Breaker y **respuesta degradada** |
| Emisión de certificado sin fuentes | no se emite nada parcial; reintento posterior |
| Resultado desconocido en la Saga | **reconciliar antes de compensar** |
| Todos los servicios | comprobaciones de salud |

**Prohibido:** reintentos ilimitados · reintentar errores funcionales · Circuit Breaker en
operaciones puramente locales · **compensar hechos irreversibles**.

## Justificación
Los fallos transitorios merecen reintento; los funcionales, no. Y ningún mecanismo de resiliencia
puede justificar revertir un hecho de negocio ya confirmado.

## Consecuencias positivas
- Degradación predecible y diagnosticable.
- Evidencia académica clara: Circuit Breaker abierto, mensaje en cola de mensajes muertos, respuesta
  degradada.

## Consecuencias negativas
- Indisponibilidad de una dependencia bloquea operaciones dependientes (por diseño).
- Más configuración que ajustar y observar.

## Riesgos residuales
Umbrales mal calibrados pueden abrir el Circuit Breaker prematuramente; se ajustarán con las métricas.

## Relación con criterios académicos
Curso 1: manejo de errores, timeouts, reintentos, Circuit Breaker. Curso 2: resiliencia,
idempotencia, continuidad ante fallos. Curso 3: recuperación y troubleshooting.

## Decisiones relacionadas
[T06](./ADR-T06-comunicacion.md) · [T09](./ADR-T09-inbox-deduplicacion.md) · [T11](./ADR-T11-composicion-de-api.md) · [T12](./ADR-T12-conjunto-de-lecciones-vigente.md) · [T13](./ADR-T13-saga-matricula-de-pago.md)
