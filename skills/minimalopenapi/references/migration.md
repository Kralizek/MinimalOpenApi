# Migrating an existing code-first ASP.NET Core API to MinimalOpenAPI

This guide is for **adopting contract-first MinimalOpenAPI in an existing application**, not for upgrading between MinimalOpenAPI versions (see [version migration](version-migration.md)). The overriding goal is to **preserve externally observable behavior while replacing endpoint definitions and implementations**. Migration is incremental: do not rewrite unrelated endpoints.

## 1. Inventory the existing API before changing it

Identify all route registrations (Minimal API delegates, controller actions, route groups), HTTP methods, route constraints and names, parameter sources/defaults, DTOs, JSON options/converters, status codes and content types. Also inspect authentication/authorization policies, endpoint filters, exception handling, validation, middleware, headers, rate limiting, antiforgery and tests.

Use the existing OpenAPI document (if any), integration/contract tests, actual HTTP responses and implementation code as evidence. **Do not assume generated code-first OpenAPI is comprehensive or authoritative**: middleware responses, custom filters and serializers may not be captured. Record inconsistencies and uncertainties instead of inventing behavior.

Create a per-endpoint migration table containing: route/method, current handler, binding, request/response payloads, observed and expected status codes, middleware-owned responses, policies, target `operationId`, support blockers, and verification plan.

## 2. Author the contract explicitly

Create an OpenAPI 3.0/3.1 YAML or JSON document and register it with an `<OpenApi Include="...">` MSBuild item. Preserve existing public routes, HTTP methods, route constraints, parameter names and sources, content types, status codes, JSON field names, casing, nullability, required fields, defaults and error shapes.

**Semantic schema ownership:** use `components/schemas` for intentionally shared **business-level concepts**; use inline operation request/response schemas for **transport-only shapes**, even when two endpoints currently have identical DTOs. This avoids coupling endpoints whose wire contracts should evolve independently. JSON inline schemas may contain nested object properties and inline arrays of objects: MOA generates typed records for them. A transport schema can `$ref` a shared business concept.

Before coding, compare every construct to [feature support](feature-support.md). In particular `oneOf`/`anyOf`, binary or text response families, certain media types and multipart arrays of objects are not supported. Do **not** silently change semantics to fit generator capabilities; document and discuss blockers.

## 3. Separate handler-owned and infrastructure-owned statuses

Include **every externally observable response** in the authored OpenAPI document. Determine who produces each status: business logic, authorization middleware, exception handling or endpoint filters. Where appropriate, use document-wide `ExcludeStatusCodesFromHandlers` and per-operation `MinimalOpenApiHandler` items; filtering affects **only generated handler result types**, never OpenAPI or endpoint metadata.

```xml
<ItemGroup>
  <OpenApi Include="openapi.yaml" ExcludeStatusCodesFromHandlers="401;403" />
  <MinimalOpenApiHandler Include="getCustomer"
                         IncludeStatusCodes="403" />
</ItemGroup>
```

The above is illustrative, **not a default**: reinclude 403 only if that handler produces it, and do not exclude 401/403 unless corresponding infrastructure is genuinely configured. For multiple documents `OpenApiFile` must match the full configured `OpenApi Include` path. Review [response ownership](response-handling.md).

## 4. Implement the generated endpoint

Install the single `MinimalOpenAPI` NuGet package, retaining existing application versions unless an upgrade was authorized; target .NET 10. Build after adding the OpenAPI document. Inspect actual generated namespaces, contract types and `HandleAsync` signatures before implementing one concrete `<OperationId>EndpointBase` per operation. Do not edit generated source or invent generated type names.

Register `builder.Services.AddMinimalOpenApi()` and `app.MapMinimalOpenApiEndpoints()` once. Transfer endpoint-specific policies into `<OperationId>EndpointConfigurationBase` when appropriate; preserve app-wide middleware and services. Do not assume declarative security metadata sets up authentication, or that generated data annotations automatically execute validation.

**Avoid duplicate routes.** Plan the cutover so old manual/controller mappings and new generated mappings do not both claim the same method/path. When migrating a portion of an application, ensure the new contract contains only the operations intended to be mapped in that stage, or use a controlled route-switching mechanism. Do not delete the old implementation before verifying replacement behavior.

## 5. Verify compatibility, then cut over

Compare the existing implementation and MOA implementation using automated HTTP-level regression tests. At minimum cover:
- Method/path/constraints, route matching, parameter binding and default values.
- Request and response JSON: exact property names and serialization rules, null/omitted fields, requiredness, status and content type.
- Successful, not-found, validation, conflict and error responses, including Problem Details where used.
- Authorized, unauthorized and forbidden requests; middleware-owned response metadata and actual runtime behavior.
- Headers, caching, filters, rate limiting, antiforgery, exception handling and custom JSON converters as relevant.

Run build, integration tests and smoke checks. Address generator diagnostics in the contract or application, not by duplicating routes or introducing guessed DTOs. Remove old registrations after the replacement is verified. Migrate one operation or cohesive route group at a time, preserving rollback options.

## Migration report template

```text
Operation: GET /customers/{id} -> operationId: getCustomer
Current: route registration / handler / input binding / serializer
Observed contract: statuses, content types, payload shape, policies
Proposed contract: inline transport response; references to shared business schemas
Response owners: handler (200/404); middleware (401/403 if confirmed)
Potential blockers: unsupported schemas/media types, custom filters/converters
Verification: baseline HTTP cases, new HTTP cases, parity results
Cutover: old mapping removed, no duplicate endpoint, rollback strategy
```

Treat the report as a reviewable proposal, not evidence of parity. A migration is complete only after real HTTP behavior is checked and remaining intentional differences are explicitly documented.
