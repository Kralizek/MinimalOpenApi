# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- **Multi-version OpenAPI support**: the system now stores the raw OpenAPI specification version from the top-level `openapi` field as a nullable `System.Version` on `OpenApiDocument.OpenApiVersion` (`null` when the field is absent or cannot be parsed). Any valid version string — including future ones such as `4.0.0` or `15.2.1` — is accepted and stored verbatim; callers compare `Major`/`Minor` against `KnownOpenApiVersions.V3_0` and `KnownOpenApiVersions.V3_1` to branch on supported versions.
- **Can/do parser selection via `OpenApiParserRequest`**: `IOpenApiParser.CanParse` now receives an `OpenApiParserRequest(Format, Version?)` value object instead of raw `(filePath, content)` strings. The generator is responsible for format detection (file extension → `OpenApiFormat` enum) and a lightweight version peek (regex on the raw content), then passes the pre-detected values to each parser. Each parser expresses its scope with a simple declarative condition — `return request.Format == OpenApiFormat.Yaml` for the current YAML parser, or `return request.Format == OpenApiFormat.Yaml && request.Version?.Major == 4` for a hypothetical future version-targeted parser. Existing parsers do not need to change when a new version-targeted parser is added.
- **OpenAPI 3.1 schema normalisation**: both the YAML and JSON parsers now accept the JSON Schema 2020-12 array type syntax (`type: ["string", "null"]`) introduced in OpenAPI 3.1 and transparently normalise it to the internal `Nullable = true` representation, so generators and downstream code remain version-agnostic.
- **`MOA006` diagnostic**: a new compile-time warning is emitted when the `openapi` version field is absent or unrecognised. Code generation still proceeds (best-effort), but the warning prompts the developer to verify their spec. Supported recognised values are `3.0.x` and `3.1.x`.
- **OpenAPI 3.1 generator integration tests** (`OpenApi31GeneratorTests`): 17 new generator-level tests covering every 3.0 → 3.1 difference: `type: ["T", "null"]` → `T?` for string/integer/Guid, `type: ["T"]` without `"null"` (non-nullable), required fields with type arrays, POST request-body nullable fields, no `MOA006` for `3.1.x` specs, parity assertions that 3.0 (`nullable: true`) and 3.1 (type array) produce identical DTOs/handlers/mappings. Both YAML and JSON formats are covered.

### Added

- `docs/consumer-agents.md`: consumer-facing agent guide for coding agents integrating this library into downstream projects.
- `<OpenApi Publish="true" />` MSBuild metadata support: spec files marked with `Publish="true"` are copied to a flat `openapi/schemas/<filename>.<ext>` directory in the build output (e.g. `openapi/schemas/clients.yaml`) and at the same relative path in `dotnet publish` output. Files are preserved byte-for-byte; YAML stays YAML, JSON stays JSON.
- `MapOpenApiSchemas()` extension method on `IEndpointRouteBuilder` in `MinimalOpenAPI.Runtime`: scans `AppContext.BaseDirectory/openapi/schemas/` at startup and registers one `GET /.openapi/schemas/{version}/{name}.{ext}` endpoint per schema file (e.g. `/.openapi/schemas/1.0.0/clients.yaml`). The `info.version` field is extracted from each file via lightweight regex; when it cannot be determined the version segment is omitted. Works for specs declared directly in the project as well as specs contributed via a NuGet contracts package (gRPC-style). Returns a `RouteGroupBuilder` for further configuration (e.g. `.RequireAuthorization()`).
- **Enum support**: the OpenAPI `enum` keyword on schema objects now generates a C# `enum` type decorated with `[JsonConverter(typeof(JsonStringEnumConverter))]`. Top-level schemas with `enum` produce a named enum in the `Contracts` namespace; inline enum schemas on object properties produce a derived enum named `{ContainingSchema}{PascalCase(PropertyName)}` (e.g. `Order.status` → `OrderStatus`). Both YAML and JSON formats are supported.
- Inline object schemas in component DTO properties now generate named top-level records instead of falling back to `object`. A property `address` on a schema `Order` produces an `OrderAddress` record in the Contracts namespace. Recursive inline objects are handled with cycle detection.
- `TypeMapper.ToPascalCase` now correctly handles snake_case (`billing_address` → `BillingAddress`), kebab-case (`due-date` → `DueDate`), camelCase (`billingAddress` → `BillingAddress`), and PascalCase inputs. This applies to all generated C# identifiers: DTO property names, nested record names, handler parameter names, and class names derived from operation IDs.

### Changed

- `format: date` string schemas now map to `global::System.DateOnly` instead of `string`. This provides compile-time safety for date-without-time fields on .NET 6+.

## 1.0.0

_Placeholder — release notes will be added on publish._

## 1.0.0-rc.1

_Placeholder — release notes will be added on publish._

## 1.0.0-beta.1

_Placeholder — release notes will be added on publish._

## 1.0.0-alpha

### Added

- Initial contract-first framework for ASP.NET Core Minimal APIs. A Roslyn source generator reads an OpenAPI spec file (YAML or JSON) at build time and generates strongly-typed DTO records, abstract handler base classes, DI registration, and endpoint mapping code.
- Five NuGet packages:
  - `MinimalOpenAPI` — main package; bundles the source generator and declares a dependency on `MinimalOpenAPI.Runtime`.
  - `MinimalOpenAPI.Runtime` — ASP.NET Core runtime services (`AddMinimalOpenApi`, `MapMinimalOpenApiEndpoints`).
  - `MinimalOpenAPI.Abstractions` — OpenAPI document model (`OpenApiDocument`, `OpenApiOperation`, `OpenApiSchema`, …) and the `IOpenApiParser` contract.
  - `MinimalOpenAPI.Parser.Yaml` — YAML OpenAPI spec parser built on YamlDotNet.
  - `MinimalOpenAPI.Parser.Json` — JSON OpenAPI spec parser built on `System.Text.Json`.
- Support for both YAML (`openapi.yaml`) and JSON (`openapi.json`) OpenAPI spec files via the `<OpenApi>` MSBuild item.
- Non-path parameters (query, header, cookie) are grouped into a nested `Parameters` record decorated with `[AsParameters]`, keeping handler signatures clean.
- Required schema properties in generated DTO records use the C# `required` keyword instead of constructor enforcement.
