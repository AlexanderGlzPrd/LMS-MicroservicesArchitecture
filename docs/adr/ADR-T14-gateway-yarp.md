# ADR-T14 — API Gateway con YARP

## Estado
Aceptado — 2026-07-16

## Contexto
Las rúbricas exigen un punto de entrada unificado con enrutamiento, seguridad y exposición
controlada, compatible con .NET, Docker, Kubernetes y Keycloak.

## Problema
¿Qué tecnología de Gateway se adopta y qué responsabilidades asume?

## Alternativas consideradas
- **Ocelot**: pensado para .NET, con funciones integradas; menor ritmo de evolución.
- **YARP**: mantenido por Microsoft, alto rendimiento, integración natural con el middleware de
  autenticación y con las capacidades de limitación de peticiones de ASP.NET Core.
- **Gateway de infraestructura externo**: potente, pero añade una tecnología ajena a la pila y
  complica la ejecución local.

## Decisión
**YARP** como API Gateway.

**Responsabilidades:** enrutamiento hacia los servicios · validación del JWT (firma, emisor,
audiencia, expiración) · propagación del token · identificador de correlación · limitación de
peticiones.

**Rutas públicas:** catálogo, contenido publicado de un curso y **verificación de certificado por
`CertificateId`**. El resto requiere autenticación y el rol correspondiente.

**Prohibido:** cualquier lógica de dominio y la composición de respuestas, que corresponde al BFF.

## Justificación
Es la opción alineada con la pila del proyecto, con menor fricción de configuración y buena
integración con Keycloak y Kubernetes.

## Consecuencias positivas
- Punto único de entrada y de validación inicial de identidad.
- Superficie pública controlada explícitamente.

## Consecuencias negativas
- Componente adicional en la ruta crítica de todas las peticiones.

## Riesgos residuales
Tentación de acumular lógica en el Gateway; se prohíbe explícitamente y se separa el BFF.

## Relación con criterios académicos
Curso 1: API Gateway. Curso 3: punto de entrada unificado, seguridad, exposición controlada.

## Decisiones relacionadas
[T11](./ADR-T11-composicion-de-api.md) · [T15](./ADR-T15-seguridad-keycloak.md) · [T18](./ADR-T18-kubernetes.md)
