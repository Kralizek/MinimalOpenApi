# Consumer-agent evaluation scenario

This is a **manual agent evaluation fixture**, not an automated AI evaluation. Run it against a fresh .NET 10 ASP.NET Core app with the `MinimalOpenAPI` package and the skill loaded. Do not give the agent access to generator internals.

## Task to give the agent

"Implement an Orders API contract-first using MinimalOpenAPI. GET /orders/{id} has 200, 401, 403 and 404; 401/403 come from the application's configured authentication/authorization layer, 404 comes from the handler. Add a second operation that declares seven distinct handler-owned result types. Ensure generated handler implementations compile and test both business responses and middleware ownership. Explain every change to the OpenAPI contract and MSBuild items."

## Additional schema-design probe

In a second evaluation, ask the agent to add an operation-specific JSON request containing a nested object and an array of inline object items, with an inline response object. Give two new endpoints with identical-looking response envelopes but **independent anticipated evolution**; verify the agent keeps these inline instead of extracting a shared component merely for deduplication. Separately, provide an **existing API** where two operations deliberately reuse a named **transport** response schema and are expected to evolve together. The agent must preserve that component and its `$ref` usage during migration rather than automatically inlining it. Also provide a shared business-level entity and verify its references remain intact. Check that it uses supported nested inline schemas and generated types rather than writing manual DTOs. Then ask about `oneOf` and `anyOf`: the agent should identify them as unsupported, distinguish them from `allOf`, and **not** silently replace their semantics.

## Code-first migration evaluation

Start from an existing ASP.NET Core Minimal API with manual `MapGet`/`MapPost` routes, transport DTOs, nested JSON payloads, authentication/authorization policies, endpoint filters or middleware, and tests of observable HTTP behavior. Give the agent the task: "Migrate one cohesive route group to contract-first MinimalOpenAPI without changing externally observable behavior. Produce an endpoint-by-endpoint migration report, implement generated handlers and endpoint configuration, and demonstrate compatibility."

Evaluate that the agent:
- inventories current routes, binding, serializer options, status codes and middleware behavior before editing;
- identifies business-level shared concepts versus operation-local transport schemas;
- preserves route, wire representation, policies, status codes and runtime behaviors in the OpenAPI contract;
- respects unsupported generator constructs and asks for a decision rather than silently changing contract meaning;
- does not leave duplicate routes after replacing the selected group;
- runs HTTP-level tests against old and new implementations, reports observed differences, and documents any unresolved parity gaps.

These assertions require an agent runner and real test application; the static skill workflow only validates documentation integrity.

## Acceptance criteria

- Contract is authored first and retains all declared response codes.
- Consumer references only the public `MinimalOpenAPI` package.
- Code uses generated handlers, not manual duplicate route registration or edited generated sources.
- 401/403 exclusions affect the GET handler signature, not its OpenAPI response metadata.
- Security middleware is actually configured for the endpoint, not assumed from filtered signatures.
- The seven-response operation compiles using generated nested `Results<...>` type, without arbitrarily filtering handler-owned statuses.
- New endpoint-specific JSON schemas remain inline when independent evolution is intended; deliberately shared existing business **or transport** schemas retain their named `components/schemas` definitions and `$ref` links; nested objects and inline array item objects use generated types. Unsupported `oneOf`/`anyOf` are explained without unsafe substitution.
- No MOA001–MOA017 diagnostic remains and `dotnet build`, application tests and endpoint checks succeed.
- The agent reports concrete validation evidence, not just a claim of success.

## Re-running after changes

Record package version, skill commit SHA, model/agent version, generated code diff, full build/test results, and whether each criterion passed. Compare before/after skill changes. The existing `sample/ResponseResults` and integration test suite exercise relevant library behaviors; this fixture evaluates **agent instructions** and requires a separate agent runner for automation.
