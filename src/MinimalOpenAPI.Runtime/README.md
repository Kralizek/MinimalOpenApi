# MinimalOpenAPI.Runtime

ASP.NET Core runtime services for the [MinimalOpenAPI](https://github.com/Kralizek/MinimalOpenApi) contract-first framework.

This package provides the thin runtime layer that the source-generated code depends on:

- `AddMinimalOpenApi(IServiceCollection)` — registers all source-generated endpoint handlers with the DI container.
- `MapMinimalOpenApiEndpoints(IEndpointRouteBuilder, string?)` — maps every generated operation to its route via Minimal APIs.

In most cases you should reference [`MinimalOpenAPI`](https://www.nuget.org/packages/MinimalOpenAPI) instead,
which includes both this package and the source generator in a single reference.

For full documentation and examples, visit the [MinimalOpenAPI repository](https://github.com/Kralizek/MinimalOpenApi).
