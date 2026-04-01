## Summary

For `string` schemas with `format: email` or `format: uri`, emit `[EmailAddress]` or `[Url]` data annotation attributes respectively, without changing the C# property type.

## Motivation

These formats are semantic constraints that can be validated with built-in `DataAnnotations` attributes. Adding them costs almost nothing but improves validation metadata and IDE hints.

## Proposed change

Extend the annotation-emitting logic (introduced in the constraint-validation-attributes issue) in `DtoGenerator` and `HandlerBaseGenerator`:

- `("string", "email")` → emit `[EmailAddress]` attribute (type stays `string`)
- `("string", "uri")` → emit `[Url]` attribute (type stays `string`)

## Affected components

- `MinimalOpenAPI` — `DtoGenerator`, `HandlerBaseGenerator`

## Notes

- **Non-breaking**: C# property type is unchanged.
- Documented in [`docs/schema-feature-roadmap.md` §4.8](docs/schema-feature-roadmap.md).
- Best implemented together with or after the constraint-validation-attributes feature.