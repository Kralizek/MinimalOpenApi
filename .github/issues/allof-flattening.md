## Summary

Support the `allOf` composition keyword by flattening properties from all subschemas into a single generated record.

## Motivation

`allOf` is the primary inheritance mechanism in OpenAPI 3.0 specs. Many real-world APIs use it to compose a base schema with additional properties. Without support, such schemas produce empty or incomplete records.

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.7](docs/schema-feature-roadmap.md).

1. Add `List<OpenApiSchema> AllOf` to `OpenApiSchema`.
2. Both parsers resolve each subschema in `allOf` (may be `$ref` or inline).
3. `DtoGenerator`: when `schema.AllOf.Count > 0`, collect and merge all properties from all subschemas into a single record. Union the `required` lists.
4. Resolve `$ref` subschemas by looking up `allSchemas`.
5. **Conflict handling**: if two subschemas declare a property with the same name and incompatible types, emit a diagnostic warning (`MOA006` proposed) and fall back to `object` for the conflicting property.

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `DtoGenerator`

## Notes

- This is a large effort; implement `allOf` first before tackling `oneOf`/`anyOf`.
- A new diagnostic code `MOA006` may be needed for conflict warnings.