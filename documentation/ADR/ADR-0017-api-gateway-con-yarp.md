# ADR-0017 — API Gateway con YARP

## Estado
Aceptado — 2026-07-16

## Contexto
El sistema necesita un punto de entrada unificado con enrutamiento, seguridad y exposición
controlada, compatible con .NET, Docker, Kubernetes y Keycloak. Sin él, cada servicio tendría que
publicar su propia superficie y repetir la validación del borde.

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

## Decisiones relacionadas
[ADR-0014](./ADR-0014-composicion-de-api-en-bff.md) · [ADR-0018](./ADR-0018-seguridad-con-keycloak.md) · [ADR-0021](./ADR-0021-despliegue-en-kubernetes.md)
