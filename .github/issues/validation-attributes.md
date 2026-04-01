## Summary

Map OpenAPI constraint keywords to `System.ComponentModel.DataAnnotations` attributes on generated DTO properties and `Parameters` record properties.

| OpenAPI keyword | C# attribute |
|---|---|
| `minLength` / `maxLength` on `string` | `[StringLength]` / `[MinLength]` / `[MaxLength]` |
| `pattern` | `[RegularExpression(pattern)]` |
| `minimum` / `maximum` on `integer` / `number` | `[Range(min, max)]` |
| `minItems` / `maxItems` on `array` | `[MinLength]` / `[MaxLength]` |

## Motivation

Constraint keywords are ubiquitous in real-world OpenAPI specs. Emitting the corresponding `DataAnnotations` attributes provides OpenAPI endpoint metadata and IDE-level hints, and integrates with filter-based validation if the consumer opts in.

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.2](docs/schema-feature-roadmap.md).

1. Add nullable fields to `OpenApiSchema`: `int? MinLength`, `int? MaxLength`, `string? Pattern`, `double? Minimum`, `double? Maximum`, `int? MinItems`, `int? MaxItems`.
2. Both parsers read these fields.
3. Create a `ValidationAttributeEmitter` helper (or extend `TypeMapper`) to convert schema constraint fields to attribute strings.
4. Apply in `DtoGenerator.GenerateRecord` and the `Parameters` record emitter in `HandlerBaseGenerator`.

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `DtoGenerator`, `HandlerBaseGenerator`

## Notes

- ASP.NET Core Minimal APIs do **not** run `DataAnnotations` validation automatically. Document this clearly in generated XML comments.
- No external packages required.