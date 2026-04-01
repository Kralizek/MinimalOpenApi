# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- `docs/consumer-agents.md`: consumer-facing agent guide for coding agents integrating this library into downstream projects.
- `<OpenApi Publish="true" />` MSBuild metadata support: spec files marked with `Publish="true"` are copied to a flat `openapi/schemas/<filename>.<ext>` directory in the build output (e.g. `openapi/schemas/clients.yaml`) and at the same relative path in `dotnet publish` output. Files are preserved byte-for-byte; YAML stays YAML, JSON stays JSON.
- `MapOpenApiSchemas()` extension method on `IEndpointRouteBuilder` in `MinimalOpenAPI.Runtime`: scans `AppContext.BaseDirectory/openapi/schemas/` at startup and registers one `GET /openapi/schemas/{version}/{name}.{ext}` endpoint per schema file (e.g. `/openapi/schemas/1.0.0/clients.yaml`). The `info.version` field is extracted from each file via lightweight regex; when it cannot be determined the version segment is omitted. Works for specs declared directly in the project as well as specs contributed via a NuGet contracts package (gRPC-style). Returns a `RouteGroupBuilder` for further configuration (e.g. `.RequireAuthorization()`).

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
