# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

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
