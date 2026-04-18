# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.0.0

### Added

- **Contract-first source generator**: a Roslyn incremental generator reads an OpenAPI YAML or JSON spec at build time and emits DTO records, abstract handler base classes, DI registration, and endpoint mapping. The developer only writes business logic.

- **OpenAPI 3.0 and 3.1 support** via YAML and JSON. Parsers are selected by format and version; the 3.1 type-array nullable syntax is normalised automatically.

- **Rich schema support**: `components/schemas` objects generate `sealed record` types; inline object schemas generate named sibling records; `enum` schemas generate C# `enum` types with `[JsonStringEnumConverter]`; `additionalProperties` maps to `Dictionary<string, T>` with a generated value record for inline object value types; `format: date` maps to `DateOnly`.

- **Validation attributes**: OpenAPI constraint keywords (`minLength`, `maxLength`, `pattern`, `minimum`, `maximum`, `minItems`, `maxItems`) are emitted as `System.ComponentModel.DataAnnotations` attributes on generated properties.

- **Parameter support**: path parameters are typed with route constraints; query, header, and cookie parameters are grouped into a `Parameters` record decorated with `[AsParameters]`.

- **Multiple spec files per project**: each `<OpenApi>` item generates code into its own `{RootNamespace}.{SpecName}` sub-namespace, with the spec name derived from the file name or the `Namespace` MSBuild item metadata.

- **Spec publishing and serving**: every `<OpenApi ... />` item is copied to build and publish output under a collision-safe internal hashed path (`openapi/schemas/<SchemaId>/<filename>.<ext>`). `MapOpenApiSchemas()` maps only schemas that declare `PublishAs`, using the exact `PublishAs` path, and returns an `OpenApiSchemaMapResult` containing `OpenApiSchemaEndpoint` descriptors for each mapped schema. Descriptor name/version are project-driven via `DisplayName` and `DisplayVersion` metadata (with fallback name = file name without extension, version = `null`), and the descriptor includes `PublicPath`, `FullName`, and `RouteHandlerBuilder` for Swagger UI wiring and endpoint customization.

- **Endpoint customizer pattern**: a generated `<OperationId>EndpointRegistration` base class lets consumers configure individual endpoints (authorization, rate limiting, etc.) without touching generated code.

- **Compile-time diagnostics** (`MOA001`–`MOA006`) for missing or duplicate handler implementations, unparseable specs, unrecognised file extensions, and unrecognised OpenAPI versions.
