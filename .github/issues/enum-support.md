## Summary

Support the OpenAPI `enum` keyword on schema objects, generating a C# `enum` type for each enum schema.

## Motivation

OpenAPI `enum` is widely used to constrain field values to a finite set. Without support, those fields are currently typed as `string` or `int`, losing all type safety and IDE guidance.

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.1](docs/schema-feature-roadmap.md).

1. Add `List<string>? Enum` to `OpenApiSchema`.
2. Both parsers (YAML + JSON) read the `enum` array.
3. `DtoGenerator` emits a C# `enum` (instead of a record) when `Enum` is non-null; applies `[JsonConverter(typeof(JsonStringEnumConverter))]`.
4. `TypeMapper.MapSchema` maps an enum schema to the generated enum name when the schema has a `$ref`.
5. **Inline enum schemas** on properties or parameters use a derived name: `{ContainingSchema}{PascalCase(PropertyName)}` (e.g. `Order.status` → `OrderStatus`).

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `DtoGenerator`, `TypeMapper`

## Notes

- No external packages required — `System.Text.Json` ships `JsonStringEnumConverter`.
- Inline enum naming convention must be stable; document it.
- Cover with generator tests and integration tests.