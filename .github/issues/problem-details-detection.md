## Summary

When a response declares `content: application/problem+json`, substitute `Microsoft.AspNetCore.Http.ProblemDetails` as the response schema type instead of emitting a custom DTO.

## Motivation

`application/problem+json` (RFC 7807 / RFC 9457) conventionally maps to `ProblemDetails`, which is built into ASP.NET Core. Emitting a custom error DTO for every error response clutters the generated Contracts namespace.

## Design questions (must be resolved before implementation)

1. **Automatic vs opt-in detection**: should this be opt-in (consumer adds a marker) or automatic (detect `application/problem+json` content type)? Automatic keeps the consumer experience simple but may surprise consumers with custom problem-details extension schemas.
2. Should the generator emit a warning when it detects `application/problem+json` and applies the substitution?

## Affected components

- `MinimalOpenAPI` — `DtoGenerator`, `HandlerBaseGenerator`

## Notes

Documented in [`docs/schema-feature-roadmap.md` §2.4.M](docs/schema-feature-roadmap.md). **Design must be agreed before implementation begins.**