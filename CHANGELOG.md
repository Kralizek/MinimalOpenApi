# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- **`additionalProperties` maps to `Dictionary<string, T>`**: when a schema uses `additionalProperties: { typed schema }` and has no named `properties`, it is now mapped to `global::System.Collections.Generic.Dictionary<string, T>` where `T` is the mapped value type. Both YAML and JSON parsers extract `additionalProperties`; `TypeMapper` emits the correct dictionary type (nullable when the property is optional); `DtoGenerator` skips generating a record for pure-dictionary component schemas. A boolean `additionalProperties: true` (allow-any) is ignored and falls back to `object`.

- **Inline complex `additionalProperties` value types**: when `additionalProperties` is itself an inline object schema (rather than a primitive type), the generator now emits a dedicated named record for the value type and uses it in the `Dictionary<string, T>` mapping. For component schemas the value record is a top-level sibling in the `Contracts` namespace named `{Schema}{Property}Value` (e.g. `additionalProperties` on `Todo.metadata` → `TodoMetadataValue` record + `Dictionary<string, TodoMetadataValue>` property). For inline request/response schemas the value record is a nested type inside the handler base class named `{InlineTypeName}{Property}Value` (e.g. `Request.labels` → `RequestLabelsValue` nested record + `Dictionary<string, RequestLabelsValue>` property). Both YAML and JSON parsers support this. Cycle detection and deduplication are maintained.

- **Validation attributes for constraint keywords**: OpenAPI constraint keywords are now mapped to `System.ComponentModel.DataAnnotations` attributes on generated DTO record properties and `Parameters` record properties. The mapping is: `minLength`/`maxLength` on `string` → `[StringLength]` / `[MinLength]` / `[MaxLength]`; `pattern` on `string` → `[RegularExpression]`; `minimum`/`maximum` on `integer`/`number` → `[Range]`; `minItems`/`maxItems` on `array` → `[MinLength]`/`[MaxLength]`. Both YAML and JSON parsers support all constraint keywords. **Note**: ASP.NET Core Minimal APIs do not run `DataAnnotations` validation automatically; the attributes provide OpenAPI endpoint metadata and IDE-level hints, and are respected if the consumer enables filter-based validation.

- **Multiple OpenAPI spec files per project with namespace isolation**: projects can now declare more than one `<OpenApi>` item. Each spec is code-generated into its own sub-namespace `{RootNamespace}.{SpecName}`, where `{SpecName}` is derived from the spec file name in PascalCase (e.g. `payment-api.yaml` → `PaymentApi`) or overridden explicitly via the `Namespace` MSBuild item metadata (`<OpenApi Include="payment.yaml" Namespace="Payment" />`). DTOs live in `{RootNamespace}.{SpecName}.Contracts`; handler bases, customizer bases, DI registration, and endpoint mapping live in `{RootNamespace}.{SpecName}.Endpoints`. Generated file hint names are prefixed with the spec name (e.g. `MinimalOpenApi.Payment.Dtos.g.cs`) to avoid MSBuild conflicts. The `RegisterEndpointMapping` delegate signature changed from `Func<IEndpointRouteBuilder, string?, RouteGroupBuilder>` to `Action<IEndpointRouteBuilder, RouteGroupBuilder>` — `MapMinimalOpenApiEndpoints` now creates the group itself and dispatches all registered spec mappings onto it, enabling all specs to share the same route prefix. A `ResetForTesting()` helper method is exposed on `ServiceCollectionExtensions` to clear registered callbacks between unit tests.

### Fixed

- `dotnet publish` no longer fails with MSB4096 ("does not define a value for metadata SchemaId") when an `<OpenApi>` item is marked `Publish="true"`. The unqualified `%(SchemaId)` reference in `AddMinimalOpenApiFilesToPublishOutput` caused MSBuild to batch over all `ResolvedFileToPublish` items (including SDK-injected ones such as `apphost` that do not carry `SchemaId`). The metadata reference is now fully qualified as `%(_MinimalOpenApiFilesToPublish.SchemaId)` so batching only occurs over the items that define it.

### Changed

- Generated namespaces now include a spec-name segment: `{RootNamespace}.Contracts` → `{RootNamespace}.{SpecName}.Contracts` and `{RootNamespace}.Endpoints` → `{RootNamespace}.{SpecName}.Endpoints` (breaking change). Existing projects using a single spec named `openapi.yaml` will need to update `using` directives to `{RootNamespace}.Openapi.Contracts` / `{RootNamespace}.Openapi.Endpoints`. Rename the spec file or add `<OpenApi ... Namespace="Api" />` to get a cleaner segment name.
- `ServiceCollectionExtensions.RegisterEndpointMapping` now accepts `Action<IEndpointRouteBuilder, RouteGroupBuilder>` instead of `Func<IEndpointRouteBuilder, string?, RouteGroupBuilder>` (breaking change — internal/generated API only).
- `ServiceCollectionExtensions.RegisterGeneratedServices` and `RegisterEndpointMapping` now accumulate all registered callbacks in a thread-safe list; calling them multiple times (once per spec) is required and expected.

- **OpenAPI 3.0 and 3.1 support**: both spec versions are now fully supported end-to-end. The `openapi` field is parsed into `OpenApiDocument.OpenApiVersion` (`System.Version?`); parsers are selected via a `OpenApiParserRequest(Format, Version?)` value object so future version-targeted parsers can be added without touching existing ones. The YAML and JSON parsers normalise the OpenAPI 3.1 type-array nullable syntax (`type: ["string", "null"]`) to the shared `Nullable = true` representation. A `MOA006` compile-time warning is emitted for absent or unrecognised version strings. 17 new generator-level integration tests (`OpenApi31GeneratorTests`) cover every 3.0 → 3.1 difference — nullable primitive/UUID/integer fields, required-field behaviour, POST request bodies, no `MOA006` for 3.1 specs, and parity assertions that 3.0 and 3.1 specs produce identical generated output — for both YAML and JSON formats.
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
