## Summary

Honour `readOnly` and `writeOnly` on schema properties when generating request/response DTO records: omit `readOnly` properties from request records and omit `writeOnly` properties from response records.

## Motivation

MinimalOpenAPI already generates separate types for request and response bodies. Respecting `readOnly`/`writeOnly` prevents consumers from accidentally setting server-computed fields (e.g. `id`, `createdAt`) in request payloads.

## Proposed implementation

1. Add `bool ReadOnly` and `bool WriteOnly` to `OpenApiSchema`.
2. Both parsers read these fields.
3. Generators filter properties when building request vs response records: skip `readOnly` properties in request records; skip `writeOnly` properties in response records.

## Affected components

- `MinimalOpenAPI.Abstractions` — `OpenApiSchema`
- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `DtoGenerator`

## Notes

- `readOnly`/`writeOnly` are **advisory** in OpenAPI 3.0; ignoring them is technically conformant. Implement on consumer demand.
- Documented in [`docs/schema-feature-roadmap.md` §2.2.G](docs/schema-feature-roadmap.md).