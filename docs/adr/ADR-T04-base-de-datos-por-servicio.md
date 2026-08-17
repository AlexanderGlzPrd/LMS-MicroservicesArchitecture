# ADR-T04 — Database per Service

## Estado
Aceptado — 2026-07-16

## Contexto
Cada Bounded Context es la única autoridad sobre sus conceptos. Compartir almacenamiento destruiría
esa autoridad y convertiría la base de datos en un contrato de integración.

## Problema
¿Cómo se organiza la persistencia entre los servicios y cómo se evidencia la separación?

## Alternativas consideradas
- **Base compartida con esquemas**: cómoda para consultar, pero rompe ownership y permite joins entre servicios.
- **Base lógica por servicio en una instancia común**: aislamiento lógico con coste de recursos bajo.
- **Instancia física por servicio**: aislamiento máximo y evidencia más fuerte; mayor coste local.

## Decisión
Una **base de datos lógica por servicio**, con usuario propio y **sin permisos cruzados**.
Topología por entorno:

| Entorno | Topología |
|---|---|
| Docker Compose | una instancia con una base y un usuario por servicio |
| Kubernetes | un StatefulSet con volumen por servicio |

Queda **prohibido**: tablas compartidas, joins entre servicios, acceso directo a bases ajenas,
repositorio compartido y usar la base como contrato de integración.

## Justificación
El aislamiento lógico con credenciales separadas preserva el ownership en ambos entornos; la
topología física más estricta se reserva para Kubernetes, que es donde vive la evidencia calificable.

## Consecuencias positivas
- Ownership verificable y evolución de esquema independiente.
- Composición de datos obligada a ocurrir por API, no por base.

## Consecuencias negativas
- No hay consultas transversales; se requiere composición explícita.
- Diferencia de topología entre entornos que debe documentarse.

## Riesgos residuales
En Compose, un fallo de configuración de permisos podría permitir acceso cruzado; se mitiga con
usuarios distintos y verificación documentada.

## Relación con criterios académicos
Curso 1: *Database per Service*, evitar base compartida. Curso 3: manifiestos y configuración.

## Decisiones relacionadas
[T05](./ADR-T05-postgresql-ef-core.md) · [T11](./ADR-T11-composicion-de-api.md) · [T18](./ADR-T18-kubernetes.md)
