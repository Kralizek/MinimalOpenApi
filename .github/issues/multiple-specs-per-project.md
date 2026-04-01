## Summary

Allow multiple `<OpenApi>` items per project by generating isolated namespaces for each spec, preventing type name collisions.

## Motivation

Some projects integrate two or more external APIs (e.g. a payment API and a shipping API) or serve multiple API versions from one project. Today only one spec is supported per project; adding a second causes type name collisions.

## Proposed design (requires discussion)

Introduce namespace isolation per spec:

- `{RootNamespace}.{SpecName}.Contracts` for DTOs
- `{RootNamespace}.{SpecName}.Endpoints` for handlers and registrations

where `SpecName` is derived from the spec file name or an explicit MSBuild item metadata attribute.

## Affected components

- `MinimalOpenAPI` — source generator (namespace allocation, spec selection)
- `MinimalOpenAPI.Runtime` — `MapMinimalOpenApiEndpoints`
- `MinimalOpenAPI` — `.targets` file (multi-item support)

## Notes

Documented in [`docs/schema-feature-roadmap.md` §2.4.N](docs/schema-feature-roadmap.md). **Design must be agreed before implementation begins.**