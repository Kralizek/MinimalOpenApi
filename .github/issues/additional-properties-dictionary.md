## Summary

When a schema uses `additionalProperties: { schema }`, map it to `Dictionary<string, T>` where `T` is the value schema type.

## Motivation

Free-form map/dictionary objects are common in APIs for labels, metadata, and extension fields. Without support, these are emitted as `object`, losing type information.

## Example

```yaml
labels:
  type: object
  additionalProperties:
    type: string
```

Should generate: `Dictionary<string, string> Labels`.

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.6](docs/schema-feature-roadmap.md).

1. Add `OpenApiSchema? AdditionalProperties` to `OpenApiSchema`.
2. Both parsers read `additionalProperties`.
3. `TypeMapper.MapSchema`: when `AdditionalProperties` is set and `Properties` is empty, map to `Dictionary<string, {MapSchema(AdditionalProperties)}>`.
4. `DtoGenerator`: treat such schemas as dictionary properties.

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `DtoGenerator`, `TypeMapper`

## Notes

- `additionalProperties: true` (allow any) is ambiguous and should be ignored (fall back to `object`).
- Only `additionalProperties: { typed schema }` can be deterministically mapped.