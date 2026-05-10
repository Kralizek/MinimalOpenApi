# MinimalOpenAPI

MinimalOpenAPI is a **contract-first** OpenAPI framework for ASP.NET Core Minimal APIs.

## Status

> Pre-release — currently targeting 1.0. APIs are subject to change until a stable release is tagged. See [release maturity](#release-maturity) for details.

---

## What problem does it solve?

In a typical ASP.NET Core Minimal API project you define routes in C# and then generate an OpenAPI document from that code (code-first). The OpenAPI document is a by-product and can drift from the actual implementation.

MinimalOpenAPI flips that: you author the OpenAPI document first, and the library generates all the C# scaffolding from it at **build time**. The document is the single source of truth — the generated code is always in sync.

|  | Code-first | Contract-first (MinimalOpenAPI) |
|---|---|---|
| Source of truth | C# code | `openapi.yaml` / `openapi.json` |
| OpenAPI document | Generated (can drift) | Authored; drives the code |
| C# scaffolding | Manual | Generated |
| Client compatibility | Loose | Enforced by the contract |

This model is useful when:
- the API contract is designed independently (e.g. with Stoplight or Swagger Editor)
- the contract must be shared with client teams before implementation starts
- the contract is versioned separately from the server code

---

## How it works

```
openapi.yaml  ──►  [MinimalOpenAPI]  ──►  Generated C#
openapi.json  ──►       (build time)          │
                                              ├─ DTO records
                                              ├─ Abstract handler base classes
                                              ├─ DI registration
                                              └─ Endpoint mapping
```

The Roslyn source generator reads the OpenAPI file at build time and emits:

- **DTO records** — neutral `sealed record` types for `components/schemas`, plus request/response-scoped variants when needed.
- **Handler base classes** — one abstract `<OperationId>EndpointBase` per operation with a strongly-typed `HandleAsync` signature.
- **DI registration** — a generated `AddGeneratedEndpoints` extension and a `[ModuleInitializer]` that wires everything up automatically.
- **Endpoint mapping** — a generated `MapEndpoints` that registers all routes.

You only write the business logic.

---

## Inspecting generated source files

If you want to inspect or check in generated code, enable Roslyn compiler-generated file emission in your project:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<ItemGroup>
  <Compile Remove="Generated/**/*.cs" />
</ItemGroup>
```

`Compile Remove` is important when the output folder is inside your project tree. MinimalOpenAPI already adds generated sources to the compilation through Roslyn, so compiling emitted `.cs` files again would cause duplicate type definitions.

This is a project-wide Roslyn feature, so it emits files from **all** source generators used by your project (for example `System.Text.Json`, regex, logging, and MinimalOpenAPI).

Roslyn places generated files under generator-specific folders below `CompilerGeneratedFilesOutputPath`. MinimalOpenAPI uses structured hint names, so its generated files are grouped under a `MinimalOpenApi/{SpecName}/...` subtree, split into `Schemas`, `Operations`, and `Infrastructure`.

Checking these files into source control is optional. Teams can ignore the whole `Generated` directory or choose to commit only specific subtrees.

---

## Runtime requirements

- **.NET 10** is required at runtime. The `MinimalOpenAPI` package targets `net10.0` for its runtime services and `netstandard2.0` for the Roslyn analyzer host.
- **ASP.NET Core** (via `Microsoft.AspNetCore.App` framework reference) is required in the consuming project.

---

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [`MinimalOpenAPI`](src/MinimalOpenAPI) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI)](https://www.nuget.org/packages/MinimalOpenAPI) | **The only package you need.** Bundles the Roslyn source generator and the ASP.NET Core runtime services (`AddMinimalOpenApi`, `MapMinimalOpenApiEndpoints`). |

The `MinimalOpenAPI.Abstractions`, `MinimalOpenAPI.Parser.Yaml`, and
`MinimalOpenAPI.Parser.Json` projects are internal implementation details — their
DLLs are bundled inside the package and are not published separately.

### Pre-release packages

Pre-release packages can be published to the GitHub Packages NuGet feed by manually running the Publish workflow:

```
https://nuget.pkg.github.com/Kralizek/index.json
```

When a GitHub Release is published, the same workflow uploads the generated packages to the release and publishes them to NuGet.org.

---

## Installation

Add the `MinimalOpenAPI` package to your ASP.NET Core project:

```shell
dotnet add package MinimalOpenAPI
```

Or manually in your `.csproj`:

```xml
<PackageReference Include="MinimalOpenAPI" Version="1.0.0-beta.1" />
```

---

## Getting started

### Minimal example

**1 — Add the package and reference your OpenAPI spec file:**

```xml
<!-- MyApi.csproj -->
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0-alpha" />
  <OpenApi Include="openapi.yaml" />  <!-- or openapi.json -->
</ItemGroup>
```

**2 — Register services and map endpoints in `Program.cs`:**

```csharp
using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
app.Run();
```

**3 — Define a minimal OpenAPI spec (`openapi.yaml`):**

```yaml
openapi: "3.0.0"
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
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Item'
        "404":
          description: Not found
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

**4 — Implement the generated handler base class:**

```csharp
// GetItemEndpoint.cs
using Microsoft.AspNetCore.Http.HttpResults;
using MyApi.Contracts;
using MyApi.Endpoints;

public sealed class GetItemEndpoint(IItemRepository repo) : GetItemEndpointBase
{
    public override async Task<Results<Ok<Item>, NotFound>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await repo.FindAsync(id, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }
}
```

That's it. No manual route registration, no manual DI wiring.

If a response is declared as `application/problem+json`, the generated base class exposes a status-specific wrapper type:

```csharp
public override Task<Results<Created<Todo>, BadRequestProblem>> HandleAsync(Request request, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Task.FromResult<Results<Created<Todo>, BadRequestProblem>>(
            new BadRequestProblem(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "The request is invalid."
            }));
    }

    // ...
}
```

---

## Supported features

| Feature | Notes |
|---------|-------|
| YAML and JSON specs | `<OpenApi Include="openapi.yaml" />` or `openapi.json` |
| OpenAPI 3.0 and 3.1 | Both versions normalised to the same internal model |
| Multiple spec files | Each spec gets its own `{RootNamespace}.{SpecName}` sub-namespace |
| DTO records | Neutral `sealed record` per `components/schemas`; scoped `FooRequest` / `FooResponse` emitted when required |
| `readOnly` / `writeOnly` filtering | Request DTOs omit `readOnly`; response DTOs omit `writeOnly` |
| `ReadWriteSchemaHandling` metadata | `Ignore`, `Auto` (default), `Split` control DTO scoping strategy |
| Enum types | `enum` schemas produce C# `enum` with `[JsonStringEnumConverter]` |
| Inline object schemas | Nested object properties produce named sibling records |
| `additionalProperties` | Maps to `Dictionary<string, T>`; inline object value types get a generated record |
| Validation attributes | `minLength`, `maxLength`, `pattern`, `minimum`, `maximum`, `minItems`, `maxItems` → `DataAnnotations` |
| `format: date` | Maps to `DateOnly` |
| Path parameters | Typed with route constraints (`{id:guid}`, `{page:int}`, …) |
| Query / header / cookie params | Grouped into a `Parameters` record with `[AsParameters]` |
| Spec publishing | Every `<OpenApi />` item is copied to build and publish output under `openapi/schemas/<SchemaId>/<filename>` |
| HTTP schema serving | `MapOpenApiSchemas()` serves only schemas with `PublishAs="..."` at that exact path |
| Endpoint customizers | Optional `<OperationId>EndpointRegistration` base for per-route metadata |

See the focused [sample projects](sample/) for end-to-end examples. Start with [BasicTodo](sample/BasicTodo/README.md) for the simplest contract-first workflow.

---

## `readOnly` / `writeOnly` handling modes

Configure behavior per `<OpenApi />` item:

```xml
<ItemGroup>
  <OpenApi Include="openapi.yaml"
           ReadWriteSchemaHandling="Auto" />
</ItemGroup>
```

Supported values:

- `Ignore`: parse flags but keep neutral DTO shape (`Foo`) in request/response signatures.
- `Auto` (default): use `FooRequest` / `FooResponse` only when direct or reachable `readOnly` / `writeOnly` differences exist.
- `Split`: always use request/response-scoped body graphs from operation roots (`FooRequest` / `FooResponse`), even when currently identical.

Example: with `Account.id: readOnly` and `Account.password: writeOnly`, generated request/response signatures use `AccountRequest` and `AccountResponse`, while neutral schemas still keep a single DTO.

---

## Benchmarks

The repository includes a BenchmarkDotNet suite that compares the generated (`WithMinimalOpenAPI`) and hand-written (`WithoutMinimalOpenApi`) Todo API implementations.

- Benchmark project: [benchmarks/Benchmark](benchmarks/Benchmark)
- Benchmark docs and latest captured results: [benchmarks/README.md](benchmarks/README.md)

Run the benchmark suite from the repository root:

```shell
dotnet run --project benchmarks/Benchmark/Benchmark.csproj -c Release
```

---

## Publishing and serving OpenAPI specs

MinimalOpenAPI treats the authored OpenAPI file as the source of truth. It does not generate a new OpenAPI document at runtime.

Every `<OpenApi />` item is copied to the build output and publish output under an internal collision-safe path:

```text
openapi/schemas/<SchemaId>/<filename>
```

This happens for all OpenAPI files, whether or not they are exposed over HTTP.

To expose a schema file as an HTTP endpoint, add `PublishAs`:

```xml
<ItemGroup>
  <OpenApi Include="openapi.yaml"
           PublishAs="/openapi/schema.yaml"
           DisplayName="Todo API"
           DisplayVersion="1.0.0" />
</ItemGroup>
```

Then serve it in `Program.cs`:

```csharp
app.MapMinimalOpenApiEndpoints();

var schemas = app.MapOpenApiSchemas(); // maps GET /openapi/schema.yaml
```

Rules:

- `PublishAs` must start with `/`.
- `PublishAs` values must be unique across all `<OpenApi />` items.
- OpenAPI files without `PublishAs` are still copied to output/publish, but are not mapped as HTTP endpoints.
- `DisplayName` and `DisplayVersion` are optional metadata for the returned schema descriptors. They are not read from the OpenAPI document.
- The optional `prefix` and `schemasDirectory` parameters on `MapOpenApiSchemas()` are legacy compatibility parameters and are ignored by explicit `PublishAs` mapping.

`MapOpenApiSchemas()` returns descriptors for the mapped schema endpoints:

```csharp
var schemas = app.MapOpenApiSchemas();
foreach (var schema in schemas.Schemas)
{
    // schema.PublicPath  -> "/openapi/schema.yaml"
    // schema.Name        -> "Todo API" or filename fallback
    // schema.Version     -> "1.0.0" or null
    // schema.Endpoint    -> RouteHandlerBuilder
}
```

You can use those descriptors to configure Swagger UI, Scalar, or another OpenAPI UI package:

```csharp
app.UseSwaggerUI(options =>
{
    foreach (var schema in schemas.Schemas)
    {
        options.SwaggerEndpoint(schema.PublicPath, schema.FullName);
    }
});
```

### Contract-package pattern

An OpenAPI spec can be shipped in a separate NuGet "contracts" package (the same pattern used by gRPC `.proto` files) and the consuming project does not need an `<OpenApi>` item of its own. See [docs/architecture.md §5.1](docs/architecture.md) for details.

---

## Multiple spec files

Each `<OpenApi>` item generates code in its own sub-namespace, preventing type-name collisions across specs:

```xml
<ItemGroup>
  <OpenApi Include="orders.yaml" />
  <OpenApi Include="payments.yaml" Namespace="Payment" />
</ItemGroup>
```

Generated namespaces:

- `{RootNamespace}.Orders.Contracts` / `{RootNamespace}.Orders.Endpoints`
- `{RootNamespace}.Payment.Contracts` / `{RootNamespace}.Payment.Endpoints`

If multiple specs could resolve to the same derived spec name (for example `apis/admin/openapi.yaml` and `apis/public/openapi.yaml`), set explicit `Namespace` metadata on one or more `<OpenApi>` items so each generated namespace segment is unique.

---

## Limitations and non-goals

- **No runtime OpenAPI generation.** MinimalOpenAPI does not generate OpenAPI documents from C# code at runtime. It serves authored spec files when `PublishAs` is configured. UI packages such as Swagger UI or Scalar can consume the mapped schema descriptors, but they are optional and not required by MinimalOpenAPI.
- **`oneOf` / `anyOf` not supported.** `allOf` object schema composition is supported by flattening composed object schemas into a single generated record. `oneOf` and `anyOf` are not yet implemented.
- **No runtime validation.** Validation attributes on generated properties are informational. ASP.NET Core Minimal APIs do not run `DataAnnotations` validation automatically.
- **No code-first path.** Use Swashbuckle, NSwag, or `Microsoft.AspNetCore.OpenApi` if you want to generate an OpenAPI document from C# code.
- **OpenAPI 2.0 (Swagger) not supported.**

---

## Troubleshooting

**Warning MOA001 — no handler implementation found**

The generator emits a *warning* when it cannot find a class that inherits from a generated `<OperationId>EndpointBase`. The app will still compile, but `HandleAsync` will throw `NotImplementedException` at runtime. Add a concrete handler class:

```csharp
public sealed class GetItemEndpoint : GetItemEndpointBase
{
    public override async Task<Results<Ok<Item>, NotFound>> HandleAsync(
        Guid id, CancellationToken cancellationToken)
        => TypedResults.NotFound();
}
```

**Build error MOA002 — multiple handler implementations**

Only one class may inherit from a given base. Remove or consolidate the duplicate.

**Build error MOA003 — multiple customizer implementations**

At most one class may inherit from a given `<OperationId>EndpointRegistration` base. Remove or consolidate the duplicate.

**Build error MOA004 — OpenAPI file could not be parsed**

Check the spec file for YAML/JSON syntax errors. Validate it with a tool like the [Swagger Editor](https://editor.swagger.io) before referencing it in the project.

**Build error MOA005 — unrecognised file extension**

Only `.yaml`, `.yml`, and `.json` are supported. Rename the file or use the correct extension in the `<OpenApi>` item.

**Warning MOA006 — unknown OpenAPI version**

The `openapi` field is absent or not recognised as a 3.0.x or 3.1.x version string. Code is still generated, but behaviour may be incorrect. Add or correct the `openapi` field at the top of the spec file (e.g. `openapi: "3.1.0"`).

**Build error MOA010 — invalid `ReadWriteSchemaHandling` value**

Supported values are `Ignore`, `Auto`, and `Split`. The default is `Auto`.

**Build error — invalid or duplicate `PublishAs` value**

`PublishAs` must start with `/`, and each published schema path must be unique across all `<OpenApi />` items.

**Handler `HandleAsync` throws `NotImplementedException` at runtime**

The base class throws `NotImplementedException` by default. Make sure your concrete handler overrides `HandleAsync` and does not call `base.HandleAsync(...)`.

**`MapMinimalOpenApiEndpoints` returns an empty route group**

This happens when the source generator did not run (e.g. the `<OpenApi>` item is missing from the project file, or the spec file path is wrong). Verify that the `<OpenApi>` item points to an existing file and that a build was performed after adding it.

---

## Release maturity

MinimalOpenAPI is currently in **pre-release**. The version scheme follows [Semantic Versioning](https://semver.org):

- `1.0.0-alpha` — initial functionality, internal testing only
- `1.0.0-beta.*` — public pre-release; APIs may still change
- `1.0.0-rc.*` — release candidate; no planned breaking changes
- `1.0.0` — stable; breaking changes only in major versions

Until `1.0.0` is tagged, minor version bumps may include breaking changes. Pin to a specific version in production use.

---

## Repository structure

```
src/
  MinimalOpenAPI/               ← MinimalOpenAPI NuGet package (generator + runtime services)
  MinimalOpenAPI.Abstractions/  ← document model & parser contracts
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser implementation
  MinimalOpenAPI.Parser.Json/   ← JSON parser implementation
sample/
  SmokeTest/           ← CI/package-consumption sample; validates the packed NuGet artifact
  BasicTodo/           ← recommended starting point; minimal contract-first Todo API
  SchemaPublishing/    ← demonstrates PublishAs, MapOpenApiSchemas(), and Swagger UI wiring
  Parameters/          ← demonstrates all parameter kinds (path, query, header, cookie, $ref)
  SchemaShapes/        ← demonstrates DTO shapes (enums, allOf, readOnly/writeOnly, additionalProperties)
  ResponseResults/     ← demonstrates typed result and problem wrapper types
  GeneratedFiles/      ← demonstrates EmitCompilerGeneratedFiles to inspect generated output
benchmarks/
  Benchmark/                    ← BenchmarkDotNet suite comparing generated vs hand-written APIs
  WithMinimalOpenAPI/           ← benchmark target app using MinimalOpenAPI-generated endpoints
  WithoutMinimalOpenApi/        ← benchmark target app using hand-written endpoints
tests/
  MinimalOpenAPI.Generator.Tests/
  MinimalOpenAPI.Runtime.Tests/
  MinimalOpenAPI.IntegrationTests/
docs/
  architecture.md               ← internals, design decisions, extensibility
  releasing.md                  ← versioning and release process
  schema-feature-roadmap.md     ← OpenAPI schema feature coverage and backlog
  consumer-agents.md            ← guide for coding agents integrating this library
```

For a deep-dive into the design, architecture, and internals see [docs/architecture.md](docs/architecture.md).

For guidance on how coding agents (e.g. GitHub Copilot) should use this library in consumer projects, see [docs/consumer-agents.md](docs/consumer-agents.md).

---

## Contributing

Contributions are welcome. Please open an issue first to discuss proposed changes.

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-change`).
3. Commit your changes — the CI pipeline enforces a warning-free build (`--warnaserror`) and runs all unit and integration tests.
4. Open a pull request against `master`.

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup details.
