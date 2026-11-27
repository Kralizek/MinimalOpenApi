# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- Binary/file responses (`application/octet-stream`, `application/pdf`, `image/*`, `audio/*`, `video/*`, or a `type: string, format: binary` schema) now generate status-specific file result wrappers such as `OkFile`, instead of a body-less result. Wildcard media types require a concrete, validated content type.
- MOA018 warning when a response declares only unsupported media types and is generated without a body.
- MOA019 error when a response `$ref` cannot be resolved to a response declared under `components/responses`.
- Every OpenAPI document generates a public entry point, `{RootNamespace}.{SpecName}.{SpecName}MinimalOpenApi`, with an idempotent `Register()` method, and marks its assembly with `[assembly: MinimalOpenApiDocument(...)]`.
- `MapMinimalOpenApiEndpoints()` logs a warning when no MinimalOpenAPI document is registered.
- Handlers and endpoint configurations can be implemented in any project that references the library declaring the document, for example the host, by deriving from the generated base class. The project needs no `<OpenApi>` item; its implementations are registered in place of the library's defaults. MOA002 and MOA003 also detect duplicates across projects.
- `Generate` metadata on `<OpenApi>` items: `Contracts` generates only the DTOs, `Endpoints` only the handler and configuration bases, mapping and registration on top of the DTOs of a referenced project generating `Contracts` for the same document; the default is both. MOA020 reports missing referenced contracts, MOA021 contracts generated from a different version of the document, and MOA022 an invalid `Generate` value.

### Changed

- Class libraries (`OutputType=Library`) no longer report MOA001 for handlers they do not implement. The application that references the library reports MOA001 when no project implements them, naming the library; other libraries and test projects do not.
- Generated endpoint mapping always looks up the operation's endpoint configuration from DI at startup, rather than only when one was found at build time.

### Fixed

- Responses declared with `$ref: '#/components/responses/{name}'`, including chains of references, now generate the same handler result types and endpoint metadata as the same response written inline. Previously they lost their payload type, problem-details wrapper and media type.
- Generated sources no longer depend on the consuming project's implicit usings or namespace layout. They now compile in `Microsoft.NET.Sdk` class libraries, in web projects with `ImplicitUsings` disabled, and when the root namespace or a handler namespace collides with a generated namespace (for example `RootNamespace=Contracts`, or a root namespace equal to the `OpenApi` `Namespace`).
- Multipart form request bodies now enforce the schema's `required` list before the handler runs. A form missing a required field or file is rejected with `400 Bad Request`, and handlers never receive a `null` `Request`. Previously a form containing none of the record's fields invoked the handler with `request == null`.
- OpenAPI documents declared in referenced class libraries are now registered by the consuming host at build time, so their endpoints, handlers, endpoint configurations and schema files are included by `AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()`. Previously they were silently missing (404) unless a library type had been used before `AddMinimalOpenApi()`.
- A required property that references a nullable component schema (OpenAPI 3.0 `nullable: true` or an OpenAPI 3.1 `"null"` type) is now generated as a nullable, non-`required` C# property, so the explicit `null` the contract allows is accepted. Previously it was generated as non-nullable and `required`, and System.Text.Json rejected `null`. Code that dereferences such properties may now see nullable warnings.

## 1.1.0

### Added

- Handler response filtering through `OpenApi.ExcludeStatusCodesFromHandlers` and per-operation `MinimalOpenApiHandler` items using `IncludeStatusCodes` / `ExcludeStatusCodes`, without removing declared endpoint response metadata.
- Recursive tail-nested ASP.NET Core `Results<>` unions for more than six distinct effective handler result types.
- Compile-time diagnostics: MOA015 for invalid handler response configuration, MOA016 for duplicate `operationId`, and MOA017 for unresolved or unsupported schema references.

### Fixed

- Body-bearing non-success responses now use the appropriate status-specific ASP.NET Core typed result when one exists.
- Body-bearing statuses without a compatible built-in typed result safely fall back to `IResult` instead of an incorrect `Ok<T>`.
- Unsupported, external, relative, missing, empty, and otherwise unresolved schema `$ref` values now fail with MOA017 before malformed generated code can be emitted.

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
