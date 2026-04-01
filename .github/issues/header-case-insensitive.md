## Summary

Header parameters already appear in the generated `Parameters` record with `[FromHeader]`. The gap is that the parser does not currently treat header names in a case-insensitive way as the HTTP specification requires, and the route constraint builder ignores them.

## Motivation

HTTP header names are case-insensitive (RFC 7230). Inconsistent casing in the OpenAPI spec vs the generated `[FromHeader(Name = ...)]` attribute can cause silent binding failures at runtime.

## Proposed fix

- Normalise header parameter names to their canonical casing when reading from the spec.
- Ensure `[FromHeader(Name = "...")]` uses the exact case from the spec but ASP.NET Core binding is always case-insensitive.

## Affected components

- `MinimalOpenAPI.Parser.Yaml` / `.Parser.Json`
- `MinimalOpenAPI` — `HandlerBaseGenerator`

## Notes

This is a refinement rather than a new feature. Documented in [`docs/schema-feature-roadmap.md` §2.2.H](docs/schema-feature-roadmap.md).