## Summary

Map the `deprecated: true` keyword on operations, schema properties, and parameters to the C# `[Obsolete]` attribute in generated code.

## Motivation

`deprecated` is a low-cost signal for consumers: it gives compile-time warnings when calling or using deprecated operations/fields without changing any runtime behaviour.

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.3](docs/schema-feature-roadmap.md).

1. Add `bool Deprecated` to `OpenApiSchema`, `OpenApiOperation`, `OpenApiParameter`.
2. Both parsers read the `deprecated` boolean from each object.
3. `DtoGenerator`: emit `[Obsolete]` before deprecated DTO record properties.
4. `HandlerBaseGenerator`: emit `[Obsolete]` before the handler base class when `operation.Deprecated == true`; emit `[Obsolete]` before deprecated `Parameters` record properties.

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`, `OpenApiOperation`, `OpenApiParameter`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `DtoGenerator`, `HandlerBaseGenerator`

## Notes

- Low implementation cost, high signal quality.
- No runtime behaviour change.