# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

## 1.0.0

### Added

- **Contract-first source generation**: a Roslyn incremental generator reads OpenAPI YAML or JSON documents at build time and emits contract records and enums, handler base classes, dependency-injection registration, endpoint mapping, and endpoint metadata.
- **OpenAPI 3.0 and 3.1 support** with YAML and JSON parsers. OpenAPI 3.1 nullable type arrays are normalized into the common document model.
- **Rich schema generation** for component and inline objects, nested inline objects, inline objects used as array items, string enums, dictionaries through `additionalProperties`, and `allOf` object flattening.
- **Directional contracts** through `ReadWriteSchemaHandling="Ignore|Auto|Split"`, including reachable `readOnly` and `writeOnly` analysis and request/response-specific contract graphs.
- **Schema validation metadata** for string, number, and array constraints. `format: email` emits `[EmailAddress]`, `format: uri` emits `[Url]`, and `format: date` maps to `DateOnly`.
- **Operation parameters** for path, query, header, and cookie declarations, including path-level parameters, reusable component parameters, operation-level overrides, route constraints, and supported default-value initializers.
- **Typed response results**, including status-specific wrappers for `application/problem+json` payloads.
- **Request-body media type tracking** so generation can deliberately distinguish JSON, multipart form data, and unsupported media types.
- **`multipart/form-data` generation** with form-bound nested request records, `IFormFile`, `IReadOnlyList<IFormFile>`, mixed file and scalar fields, and nested object binding through dotted form keys.
- **Multiple OpenAPI documents per project**, each isolated in a document-specific generated namespace with optional `Namespace` item metadata.
- **Authored schema publishing**: OpenAPI documents are copied to build and publish output; documents with `PublishAs` can be served through `MapOpenApiSchemas()` and exposed to Swagger UI, Scalar, or another viewer.
- **Endpoint configuration hooks** through generated abstract `<OperationId>EndpointConfigurationBase` classes for authorization, rate limiting, antiforgery, request limits, and other endpoint policies. Application configuration runs after contract-derived endpoint metadata.
- **Generated-source inspection support** with stable, structured Roslyn hint names below `MinimalOpenApi/{SpecName}/`.
- **Deterministic schema-name normalization** across component declarations, `$ref` resolution, scoped contracts, inline types, handler signatures, and endpoint mapping. TypeSpec-style dotted names and other separator-based names now produce valid PascalCase C# identifiers.
- **Compile-time diagnostics `MOA001`–`MOA014`** for missing or duplicate implementations, invalid documents or configuration, unresolved references, unsupported multipart shapes, duplicate spec names, and component or generated-symbol collisions.
- **Focused samples and package-consumption smoke tests** covering the primary generator and runtime capabilities.

### Changed

- Prerelease `<OperationId>EndpointRegistration` types were replaced by abstract `<OperationId>EndpointConfigurationBase` types. Existing prerelease consumers must update the inherited base type and implement `Configure(RouteHandlerBuilder endpoint)`.
- The `MinimalOpenAPI` package now contains both the Roslyn source generator and the required ASP.NET Core runtime services. Parser and abstraction assemblies are bundled as implementation details and are not published separately.
- Generated handler signatures, contracts, and endpoint mappings consistently use the same normalized schema-name map.
- OpenAPI files are copied to collision-safe internal output paths and are exposed over HTTP only when an explicit `PublishAs` value is configured.

### Known limitations

- `oneOf` and `anyOf` schema composition are not supported.
- Generated data annotations are metadata; ASP.NET Core Minimal APIs do not automatically execute `DataAnnotations` validation.
- Cookie parameters require application-specific binding or access through `HttpContext`.
- Multipart array-of-object and dictionary fields are rejected with `MOA011` because they cannot be represented reliably by generated ASP.NET Core form binding.
- MinimalOpenAPI does not generate an OpenAPI document from C# at runtime and does not support OpenAPI 2.0 documents.
