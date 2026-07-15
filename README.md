# MinimalOpenAPI

[![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI)](https://www.nuget.org/packages/MinimalOpenAPI)

MinimalOpenAPI is a **contract-first OpenAPI framework for ASP.NET Core Minimal APIs**.

You author an OpenAPI document first. At build time, MinimalOpenAPI reads the document and generates the C# contracts, handler base classes, dependency-injection registration, and endpoint mapping needed to implement the API.

The OpenAPI document remains the source of truth; application code supplies only the business logic.

## Requirements

- .NET 10
- ASP.NET Core
- An OpenAPI 3.0 or 3.1 document in YAML or JSON

## Installation

```shell
dotnet add package MinimalOpenAPI --version 1.0.0
```

Or add the package and OpenAPI document directly to the project file:

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

`MinimalOpenAPI` is the only package consumers need. The parser and abstraction assemblies are bundled with the source generator and are not published as separate packages.

For a focused walkthrough from installation to the first generated endpoint, see [Getting started](docs/getting-started.md).

## Quick start

### 1. Define the contract

```yaml
openapi: "3.1.0"
info:
  title: Items API
  version: "1.0.0"
paths:
  /items/{id}:
    get:
      operationId: getItem
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: Item found
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Item"
        "404":
          description: Item not found
components:
  schemas:
    Item:
      type: object
      required: [id, name]
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
```

### 2. Register MinimalOpenAPI

```csharp
using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
app.Run();
```

### 3. Implement the generated handler base class

```csharp
public sealed class GetItemEndpoint(IItemRepository repository)
    : GetItemEndpointBase
{
    public override async Task<Results<Ok<Item>, NotFound>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await repository.FindAsync(id, cancellationToken);
        return item is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(item);
    }
}
```

No manual route registration or handler registration is required.

## What gets generated

For each OpenAPI document, the incremental source generator emits:

- contract records and enums;
- request- and response-specific contract variants when required;
- one `<OperationId>EndpointBase` handler base class per operation;
- typed `HandleAsync` signatures and result unions;
- dependency-injection registration;
- endpoint mapping and OpenAPI metadata;
- an optional abstract `<OperationId>EndpointConfigurationBase` hook for final application configuration.

With multiple documents, generated code is isolated under a document-specific namespace:

```text
{RootNamespace}.{SpecName}.Contracts
{RootNamespace}.{SpecName}.Endpoints
```

Use the `Namespace` item metadata when two documents would otherwise resolve to the same spec name:

```xml
<ItemGroup>
  <OpenApi Include="orders.yaml" />
  <OpenApi Include="payments.yaml" Namespace="Payments" />
</ItemGroup>
```

## OpenAPI item metadata

| Metadata | Purpose |
|---|---|
| `Namespace` | Overrides the generated document namespace segment. |
| `ReadWriteSchemaHandling` | Controls request/response contract splitting: `Ignore`, `Auto`, or `Split`. |
| `PublishAs` | Exposes the authored document at an exact HTTP path through `MapOpenApiSchemas()`. |
| `DisplayName` | Optional display name returned by the schema descriptor. |
| `DisplayVersion` | Optional display version returned by the schema descriptor. |

Example:

```xml
<OpenApi Include="openapi.yaml"
         Namespace="PublicApi"
         ReadWriteSchemaHandling="Auto"
         PublishAs="/openapi/schema.yaml"
         DisplayName="Public API"
         DisplayVersion="1.0.0" />
```

## Supported features

| Area | Support |
|---|---|
| Documents | OpenAPI 3.0 and 3.1; YAML and JSON; multiple documents per project. |
| Object schemas | Component and inline object records, nested inline objects, and inline objects used as array items. |
| Composition | `allOf` object composition is flattened into one generated contract. |
| Enums | String enums generate C# enums with `JsonStringEnumConverter`. |
| Dictionaries | `additionalProperties` maps to `Dictionary<string, T>`. |
| Formats | Includes `uuid`, `date-time`, `date`, numeric formats, `email`, and `uri`. |
| Constraints | Data-annotation metadata for string, number, and array constraints. |
| Directionality | `readOnly` and `writeOnly` properties can produce request/response-specific contract graphs. |
| Parameters | Typed path parameters with route constraints; query, header, reusable component, and path-level parameters. |
| Defaults | Supported parameter defaults generate C# property initializers. |
| JSON bodies | Component and inline request/response schemas with typed handler signatures. |
| Problem details | `application/problem+json` responses generate status-specific typed wrappers. |
| Multipart forms | Form-bound request records, `IFormFile`, multiple files, and nested object fields. |
| Endpoint policies | Per-operation endpoint configurations for authorization, rate limiting, antiforgery, request limits, and other metadata. |
| Schema publishing | Authored documents are copied to build/publish output and can be served at explicit paths. |
| Schema names | OpenAPI schema keys are normalized deterministically into valid C# identifiers; collisions produce diagnostics. |

## `readOnly` and `writeOnly`

The default mode is `Auto`:

```xml
<OpenApi Include="openapi.yaml"
         ReadWriteSchemaHandling="Auto" />
```

- `Ignore` keeps one neutral contract shape.
- `Auto` creates request or response variants only when the reachable schema graph differs.
- `Split` always creates request and response graphs from operation body roots.

For example, a schema with a `readOnly` identifier and a `writeOnly` password can produce `AccountRequest` and `AccountResponse` contracts while retaining a neutral `Account` contract.

## File uploads

A `multipart/form-data` request body generates a form-bound nested `Request` record. Binary string fields map to `IFormFile`; arrays of binary strings map to `IReadOnlyList<IFormFile>`.

```yaml
requestBody:
  required: true
  content:
    multipart/form-data:
      schema:
        type: object
        required: [file]
        properties:
          file:
            type: string
            format: binary
          description:
            type: string
```

Nested object fields use ASP.NET Core dotted form keys such as `metadata.title`.

Antiforgery, request-size limits, file validation, and storage policies remain application responsibilities. Configure them through normal ASP.NET Core services, middleware, and the generated endpoint configuration.

Array-of-object and dictionary form fields are not supported by ASP.NET Core's generated form binding and produce diagnostic `MOA011`.

## Publishing authored schemas

Every `<OpenApi />` document is copied to build and publish output under a collision-safe internal path:

```text
openapi/schemas/<SchemaId>/<filename>
```

Documents are exposed over HTTP only when `PublishAs` is configured:

```csharp
app.MapMinimalOpenApiEndpoints();
var schemas = app.MapOpenApiSchemas();
```

`MapOpenApiSchemas()` returns descriptors containing `PublicPath`, `Name`, `Version`, `FullName`, and `Endpoint`. These descriptors can be used to configure Swagger UI, Scalar, or another OpenAPI viewer without generating a second document at runtime.

## Inspecting generated source

Enable standard Roslyn generated-file emission:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<ItemGroup>
  <Compile Remove="Generated/**/*.cs" />
</ItemGroup>
```

MinimalOpenAPI organizes emitted files beneath `MinimalOpenApi/{SpecName}/`, split into contracts, operations, and infrastructure.

## Samples

| Sample | Demonstrates |
|---|---|
| [`BasicTodo`](sample/BasicTodo) | Minimal contract-first API. |
| [`Parameters`](sample/Parameters) | Path, query, header, cookie, reusable, and path-level parameters. |
| [`SchemaShapes`](sample/SchemaShapes) | Enums, `allOf`, dictionaries, validation metadata, and read/write splitting. |
| [`ResponseResults`](sample/ResponseResults) | Typed results and problem-details wrappers. |
| [`SchemaPublishing`](sample/SchemaPublishing) | `PublishAs`, `MapOpenApiSchemas()`, and Swagger UI integration. |
| [`GeneratedFiles`](sample/GeneratedFiles) | Inspecting compiler-generated files. |
| [`MultipartNested`](sample/MultipartNested) | File uploads and nested form objects. |
| [`SmokeTest`](sample/SmokeTest) | Clean downstream consumption of the packed NuGet artifact. |

The repository also includes a BenchmarkDotNet comparison under [`benchmarks/`](benchmarks/).

## Limitations and non-goals

- MinimalOpenAPI does not generate an OpenAPI document from C# at runtime.
- `oneOf` and `anyOf` are not currently supported.
- Generated data annotations are metadata; ASP.NET Core Minimal APIs do not automatically execute `DataAnnotations` validation.
- Cookie parameters are surfaced in the generated parameter model but require application-specific binding or access through `HttpContext`.
- OpenAPI 2.0 documents are not supported.

See [`docs/schema-feature-roadmap.md`](docs/schema-feature-roadmap.md) for the detailed support matrix and post-1.0 roadmap.

## Diagnostics

The generator reports stable `MOA001`–`MOA014` diagnostics for missing implementations, duplicate handlers or configurations, parser failures, unsupported configuration, unresolved references, unsupported multipart shapes, and generated-name collisions.

See [`docs/architecture.md`](docs/architecture.md) for the complete diagnostic table and internal design.

## Versioning and releases

MinimalOpenAPI follows [Semantic Versioning](https://semver.org/). Starting with `1.0.0`, breaking public API or generated-code changes require a major version.

Versions are derived from Git tags through MinVer. Release instructions are documented in [`docs/releasing.md`](docs/releasing.md), and notable changes are recorded in [`CHANGELOG.md`](CHANGELOG.md).

## Contributing

Contributions are welcome. Read [`CONTRIBUTING.md`](CONTRIBUTING.md) before opening a pull request. Changes to generated behavior should include tests and corresponding documentation.

Coding agents working inside this repository should also read [`AGENTS.md`](AGENTS.md). Agents consuming MinimalOpenAPI in another project should use [`docs/consumer-agents.md`](docs/consumer-agents.md).
