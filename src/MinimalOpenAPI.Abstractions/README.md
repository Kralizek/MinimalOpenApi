# MinimalOpenAPI.Abstractions

OpenAPI document model and parser abstractions for the [MinimalOpenAPI](https://github.com/Kralizek/MinimalOpenApi) contract-first framework.

This package contains the shared interfaces and model types used across all MinimalOpenAPI components:

- `IOpenApiParser` — contract for pluggable OpenAPI document parsers.
- `OpenApiDocument`, `OpenApiOperation`, `OpenApiParameter`, `OpenApiSchema`, and related model classes.

Depend on this package directly only when building a custom OpenAPI parser or extending the framework.
Most applications should reference [`MinimalOpenAPI`](https://www.nuget.org/packages/MinimalOpenAPI) instead,
which brings in everything needed.

For full documentation and examples, visit the [MinimalOpenAPI repository](https://github.com/Kralizek/MinimalOpenApi).
