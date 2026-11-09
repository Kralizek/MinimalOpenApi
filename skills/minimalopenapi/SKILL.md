---
name: minimalopenapi
description: Implement and maintain contract-first ASP.NET Core Minimal APIs using the MinimalOpenAPI NuGet package, its OpenAPI-generated handlers, response ownership rules, and diagnostics.
---

# MinimalOpenAPI consumer skill

Use this skill when a project references `MinimalOpenAPI` or asks to implement a contract-first ASP.NET Core Minimal API using it. This is **consumer guidance**, not instructions for changing the MinimalOpenAPI generator repository.

## Workflow

1. Inspect the application's existing package version, target framework, OpenAPI documents, and route/DI registration before making changes. Require .NET 10 and OpenAPI 3.0/3.1 YAML or JSON.
2. Use the **single** `MinimalOpenAPI` NuGet package. For new projects select an appropriate current stable version; do not mechanically install an example's historical version. Preserve the chosen version when editing an existing application unless an upgrade is requested.
3. Edit the authored OpenAPI contract first. Give each operation a unique `operationId`, then register the document using an `<OpenApi Include="..." />` MSBuild item.
4. Prefer inline request/response object schemas for endpoint-specific types, including nested objects and inline array item objects. Extract a `components/schemas` definition when the shape is genuinely reused or represents an intentional shared contract; see [generated types](references/generated-types.md).
5. Check [unsupported and partial OpenAPI constructs](references/feature-support.md) before using polymorphism, unusual media types, or advanced binding features. Never silently alter the contract to hide an unsupported construct.
6. Build to inspect generated types and `HandleAsync` signatures. Do not guess names or signatures and never edit generated C#.
7. Implement one concrete `<OperationId>EndpointBase` per operation. Register `builder.Services.AddMinimalOpenApi()` and `app.MapMinimalOpenApiEndpoints()` once; do not duplicate routes or register generated handlers manually.
8. Separate contract responses (every HTTP status a caller may observe) from **handler-owned** responses. Before filtering, identify which layer actually produces each status; see [response handling](references/response-handling.md).
9. Use generated `<OperationId>EndpointConfigurationBase` for endpoint policies such as authorization and antiforgery. Rebuild and address all generator diagnostics before considering work complete.
10. Validate the built application and exercise affected endpoints. Consult repository [samples](../../sample/README.md) for concrete shapes.

## Load only what is needed

- [Configuration and installation](references/configuration.md) — MSBuild metadata, multiple documents, publishing, and wiring.
- [Generated types](references/generated-types.md) — inline and nested schemas, binding, responses, forms, directional schemas, namespaces.
- [Feature support and limitations](references/feature-support.md) — unsupported OpenAPI constructs and partial runtime support.
- [Response handling](references/response-handling.md) — filtering, middleware ownership, and nested `Results<>`.
- [Diagnostics](references/diagnostics.md) — troubleshooting MOA001–MOA017.
- [Migration](references/migration.md) — using 1.1 features and avoiding assumptions from 1.0.

## Hard constraints

- The OpenAPI document remains the API contract; generated C# is not the source of truth.
- Handler filtering **never removes** authored contract responses or generated endpoint metadata.
- Do not replace `oneOf`/`anyOf` with `allOf` or discard unsupported semantics without an explicit design decision.
- Do not suppress diagnostics, replace generated models with guesses, or add parallel manual routes to bypass a build problem.
- Do not filter a status merely to shorten a result union: only filter statuses truly produced outside the handler.
- Do not claim compilation or runtime behavior was verified until the relevant build and tests have completed.
