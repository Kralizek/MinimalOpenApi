# Consumer-agent evaluation scenario

This is a **manual agent evaluation fixture**, not an automated AI evaluation. Run it against a fresh .NET 10 ASP.NET Core app with the `MinimalOpenAPI` package and the skill loaded. Do not give the agent access to generator internals.

## Task to give the agent

"Implement an Orders API contract-first using MinimalOpenAPI. GET /orders/{id} has 200, 401, 403 and 404; 401/403 come from the application's configured authentication/authorization layer, 404 comes from the handler. Add a second operation that declares seven distinct handler-owned result types. Ensure generated handler implementations compile and test both business responses and middleware ownership. Explain every change to the OpenAPI contract and MSBuild items."

## Acceptance criteria

- Contract is authored first and retains all declared response codes.
- Consumer references only the public `MinimalOpenAPI` package.
- Code uses generated handlers, not manual duplicate route registration or edited generated sources.
- 401/403 exclusions affect the GET handler signature, not its OpenAPI response metadata.
- Security middleware is actually configured for the endpoint, not assumed from filtered signatures.
- The seven-response operation compiles using generated nested `Results<...>` type, without arbitrarily filtering handler-owned statuses.
- No MOA001–MOA017 diagnostic remains and `dotnet build`, application tests and endpoint checks succeed.
- The agent reports concrete validation evidence, not just a claim of success.

## Re-running after changes

Record package version, skill commit SHA, model/agent version, generated code diff, full build/test results, and whether each criterion passed. Compare before/after skill changes. The existing `sample/ResponseResults` and integration test suite exercise relevant library behaviors; this fixture evaluates **agent instructions** and requires a separate agent runner for automation.
