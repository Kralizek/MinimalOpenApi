## Summary

Map the `default` keyword on parameter schemas to C# property initialisers in the generated `Parameters` record.

## Motivation

Pagination and filtering parameters almost always have sensible defaults (e.g. `pageSize: 20`). Without this feature, consumers must manually specify defaults in their handlers.

## Example

```yaml
parameters:
  - name: pageSize
    in: query
    schema:
      type: integer
      default: 20
```

Should generate:

```csharp
[FromQuery(Name = "pageSize")]
public int PageSize { get; init; } = 20;
```

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.5](docs/schema-feature-roadmap.md).

1. Add `string? Default` to `OpenApiSchema` (stored as a raw string).
2. Both parsers read the `default` field.
3. The Parameters record emitter writes `= <CSharpLiteral(default)>` after the property type, using a `TypeMapper.GetDefaultValue` helper or similar.

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `HandlerBaseGenerator`