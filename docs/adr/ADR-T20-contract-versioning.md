# ADR-T20 — Contratos de mensajes y versionado

## Estado
Aceptado — 2026-07-16

## Contexto
Los servicios intercambian mensajes cuyos contratos evolucionan. En un monorepo es fácil crear
acoplamiento accidental si el consumidor depende directamente del modelo del productor.

## Problema
¿Cómo se definen, comparten y versionan los contratos sin perder autonomía?

## Alternativas consideradas
- **Contratos compartidos como clases de dominio**: prohibido; filtra el modelo entre contextos.
- **Referencia directa sin reglas**: obliga a despliegues coordinados y facilita cambios rompientes.
- **Paquetes versionados independientes**: máxima autonomía; sobrecarga para un monorepo académico.
- **Referencia en el monorepo con reglas estrictas de versionado y confinamiento**.

## Decisión
Cada productor publica sus contratos en un proyecto propio de contratos, con estas reglas:

1. **Versión en el propio tipo o espacio de nombres** (`V1`). Un cambio rompiente crea `V2`; nunca se
   modifica `V1`.
2. **Compatibilidad solo aditiva** dentro de una versión: prohibido eliminar, renombrar o cambiar el
   tipo de un campo.
3. **Referencia de proyecto permitida** dentro del monorepo, con una regla dura: **el tipo de
   contrato no puede salir de Infrastructure/ACL**; el consumidor lo mapea de inmediato a su modelo
   interno.
4. **Pruebas de contrato dirigidas por el consumidor**, que verifican las expectativas mínimas.
5. **Evolución:** extraer a paquetes versionados si el repositorio se divide.

**Contracts no puede contener** Aggregate Roots, Entities, Value Objects de dominio, enumeraciones
internas ni repositorios: solo objetos de transferencia con tipos simples y los metadatos del sobre.

## Justificación
La regla de confinamiento evita el acoplamiento real aunque exista una referencia física, y el
versionado aditivo permite evolucionar sin despliegues coordinados.

## Consecuencias positivas
- Evolución independiente de productores y consumidores.
- Modelos internos protegidos de cambios ajenos.

## Consecuencias negativas
- Mapeos adicionales en la frontera.
- Disciplina necesaria: la referencia física no impide, por sí sola, el mal uso.

## Riesgos residuales
Que un tipo de contrato se filtre a Application o Domain; se vigila en revisión y con pruebas.

## Relación con criterios académicos
Curso 2: contratos de Integration Events, publicación y consumo correctos.

## Decisiones relacionadas
[T02](./ADR-T02-monorepo.md) · [T03](./ADR-T03-clean-architecture.md) · [T07](./ADR-T07-rabbitmq-masstransit.md) · [T22](./ADR-T22-technical-building-blocks.md)
