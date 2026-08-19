# ADR-0027 — Versionado de las APIs REST

## Estado
Aceptado — 2026-08-12

## Contexto
Los servicios exponen APIs REST consumidas por el gateway (ADR-0017), por otros servicios mediante
composición (ADR-0014) y por clientes externos. [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) versiona los
contratos de mensajería asíncrona, pero no dice nada de los contratos HTTP síncronos: hasta ahora
`course-authoring` publicaba rutas sin versión (`POST /courses`).

## Problema
¿Cómo evolucionar un contrato HTTP con cambios rompientes sin obligar a un despliegue coordinado de
todos sus consumidores?

## Alternativas consideradas
- **Sin versionado**: cualquier cambio rompiente obliga a desplegar servicio y clientes a la vez.
- **Cabecera `X-Api-Version`**: la URL queda estable, pero la versión es invisible en logs, trazas y
  cachés intermedias, y complica probar con `curl` o desde el navegador.
- **Negociación por tipo de medio** (`Accept: application/json;v=1`): correcto según REST, pero el
  más incómodo de operar, de documentar y de reproducir desde un cliente.
- **Segmento en la ruta** (`/api/v1/...`): versión explícita en la URL.

## Decisión
Versionado por **segmento de ruta**, con la biblioteca `Asp.Versioning` (paquetes
`Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer` y `Asp.Versioning.OpenApi`):

1. **Formato de ruta**: `/api/v{version:apiVersion}/<recurso>`. Los controladores declaran su versión
   con `[ApiVersion("1.0")]`; la versión por defecto es `1.0` y el lector es `UrlSegmentApiVersionReader`.
2. **Solo la parte mayor en la URL**: `GroupNameFormat = "'v'VVV"` produce `v1`, no `v1.0`. Los
   cambios aditivos no cambian la ruta.
3. **Un documento OpenAPI por versión**: `MapOpenApi().WithDocumentPerVersion()` sirve
   `/openapi/v1.json`, `/openapi/v2.json`, etc.; `SubstituteApiVersionInUrl` sustituye el parámetro
   de plantilla por la versión concreta en la documentación.
4. **`ReportApiVersions = true`**: las respuestas incluyen `api-supported-versions`, para que un
   cliente descubra las versiones vivas sin leer la documentación.
5. **Endpoints operativos sin versión**: `/health`, `/openapi` y Scalar no se versionan; no son
   contrato de negocio.
6. **Regla de evolución**, alineada con [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md): dentro de una versión
   solo se permiten cambios **aditivos** (nuevos campos opcionales, nuevos endpoints). Eliminar o
   renombrar un campo, cambiar su tipo o cambiar el significado de un código de estado exige `v2`;
   `v1` no se modifica.
7. **Retirada**: una versión antigua se mantiene hasta que sus consumidores migran, y se marca antes
   como obsoleta (`[ApiVersion("1.0", Deprecated = true)]`, que se refleja en `api-deprecated-versions`).

## Justificación
El segmento de ruta hace la versión visible en logs, trazas, reglas de enrutamiento de YARP y
métricas, sin configuración adicional. La misma disciplina aditiva que ya rige los mensajes
(ADR-0023) se aplica al canal síncrono, de modo que hay **una sola regla de compatibilidad** en el
sistema, con dos mecanismos de expresión.

## Consecuencias positivas
- Productor y consumidores pueden desplegarse por separado ante un cambio rompiente.
- El gateway puede enrutar o exponer versiones distintas a clientes distintos con una regla de ruta.
- La documentación queda separada por versión, sin mezclar contratos incompatibles.

## Consecuencias negativas
- Las rutas anteriores sin versión dejan de existir: `POST /courses` pasa a `POST /api/v1/courses`
  y devuelve `404`. Como el servicio todavía no tiene consumidores desplegados, no se publica ruta
  de compatibilidad.
- Mantener varias versiones vivas duplica controladores y pruebas mientras dure la migración.
- Una petición con una versión inexistente (`/api/v2/...` sin controlador) responde `404`, no un
  `400` descriptivo, porque la restricción de ruta ni siquiera llega al selector de versión.

## Riesgos residuales
Que se cuele un cambio rompiente dentro de `v1` por descuido: la restricción es de disciplina, no del
compilador. Se vigila en revisión y con pruebas de contrato dirigidas por el consumidor (ADR-0023, ADR-0024).

## Decisiones relacionadas
[ADR-0014](./ADR-0014-composicion-de-api-en-bff.md) · [ADR-0017](./ADR-0017-api-gateway-con-yarp.md) · [ADR-0023](./ADR-0023-contratos-de-mensajes-y-versionado.md) · [ADR-0024](./ADR-0024-estrategia-de-pruebas.md)
