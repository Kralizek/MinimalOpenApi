## Summary

Expose a customisation hook that lets consumers map OpenAPI `security` requirements to ASP.NET Core authorization (e.g. `.RequireAuthorization(policyName)`), without auto-generating hard-coded `[Authorize]` attributes.

## Motivation

Many real-world specs declare security requirements per operation. Consumers need a way to enforce these requirements in generated endpoint registrations without hand-editing generated code.

## Design options (must be chosen before implementation)

1. Always emit `.RequireAuthorization()` when any security requirement is present (coarse-grained, simple but imprecise).
2. Emit `.RequireAuthorization(schemeName)` and require the consumer to configure a matching policy (fine-grained but fragile without spec conventions).
3. **Preferred**: expose a customisation hook via `EndpointRegistration` — keep the library policy-agnostic and let the consumer wire up authorization logic.

## Affected components

- `MinimalOpenAPI` — `EndpointMappingGenerator`
- `MinimalOpenAPI.Runtime` — `MapMinimalOpenApiEndpoints`

## Notes

Documented in [`docs/schema-feature-roadmap.md` §2.4.L](docs/schema-feature-roadmap.md). **Design must be agreed before implementation begins.**