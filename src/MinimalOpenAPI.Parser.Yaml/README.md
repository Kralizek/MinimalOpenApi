# MinimalOpenAPI.Parser.Yaml

YAML parser for OpenAPI specifications, part of the [MinimalOpenAPI](https://github.com/Kralizek/MinimalOpenApi) contract-first framework.

This package implements `IOpenApiParser` from `MinimalOpenAPI.Abstractions` using [YamlDotNet](https://github.com/aaubry/YamlDotNet).
It supports OpenAPI 3.0 documents in `.yaml` / `.yml` format.

The parser is bundled inside the `MinimalOpenAPI.Generator` NuGet package and runs inside the Roslyn
analyzer load context at build time. You do not need to reference this package directly in most cases —
it is included transitively when you reference [`MinimalOpenAPI`](https://www.nuget.org/packages/MinimalOpenAPI).

For full documentation and examples, visit the [MinimalOpenAPI repository](https://github.com/Kralizek/MinimalOpenApi).
