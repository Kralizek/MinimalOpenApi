# MinimalOpenAPI — Architecture & Design

> **Audience**: human contributors and AI agents working on this repository.
> This document is the authoritative reference for how the library works, why it
> was designed the way it was, and how to extend it.

---

## 1. Intent

MinimalOpenAPI is a **contract-first** code-generation framework for ASP.NET Core
Minimal APIs.

The central idea is that the OpenAPI (YAML) file is the single source of truth.
Everything that can be derived from it — DTO records, abstract handler base
classes, DI registration, and endpoint mapping — is generated automatically at
build time by a Roslyn incremental source generator.  The developer only writes
the code that *cannot* be derived: the business logic inside each handler.

### Why contract-first?

The mainstream alternative is code-first: you write the C# endpoints and
annotate them with attributes, and a tool like Swashbuckle or NSwag generates
the OpenAPI document from the code.  Contract-first inverts that relationship:

| | Code-first | Contract-first (MinimalOpenAPI) |
|---|---|---|
| Source of truth | C# code | `openapi.yaml` |
| OpenAPI document | Generated (may drift) | Authored by hand or design tool |
| C# scaffolding | Manual | Generated |
| Client compatibility | Loose | Enforced — the contract drives the code |

Contract-first is useful when the API is designed independently (e.g. with
Stoplight or Swagger Editor), shared with clients before implementation, or
versioned separately from the server code.

---

## 2. Repository layout

```
src/
  MinimalOpenAPI/               ← meta-package (NuGet entry point)
  MinimalOpenAPI.Runtime/       ← runtime: AddMinimalOpenApi, MapMinimalOpenApiEndpoints
  MinimalOpenAPI.Generator/     ← Roslyn incremental source generator
  MinimalOpenAPI.Abstractions/  ← OpenAPI document model + IOpenApiParser
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser (IOpenApiParser implementation)
sample/
  MinimalOpenAPI.Sample.Api/    ← end-to-end working example (Todo CRUD)
tests/
  MinimalOpenAPI.Generator.Tests/   ← generator unit tests (Roslyn driver)
  MinimalOpenAPI.Runtime.Tests/     ← runtime unit tests
  MinimalOpenAPI.IntegrationTests/  ← WebApplicationFactory integration tests
```

---

## 3. End-to-end data flow

```
openapi.yaml
    │
    │  MSBuild target AddMinimalOpenApiFilesToAdditionalFiles
    │  promotes <OpenApi> items → AdditionalFiles with
    │  build_metadata.AdditionalFiles.MinimalOpenApiFile = true
    │
    ▼
MinimalOpenAPI.Generator  (IIncrementalGenerator, runs inside Roslyn)
    │
    ├─ Reads AdditionalFiles tagged with MinimalOpenApiFile = true
    ├─ Selects parser by file extension (currently .yaml/.yml only)
    ├─ Calls IOpenApiParser.ParseAsync → OpenApiDocument
    ├─ Scans user-project class declarations (SyntaxProvider)
    │   to discover concrete handler and customizer implementations
    │
    ├─ Emits per-operation source files:
    │   MinimalOpenApi.<OperationId>Endpoint.g.cs          (handler base class)
    │   MinimalOpenApi.<OperationId>EndpointRegistration.g.cs (customizer base)
    │
    ├─ Emits shared source files:
    │   MinimalOpenApi.Dtos.g.cs                 (DTO records)
    │   MinimalOpenApi.DependencyInjection.g.cs  (AddGeneratedEndpoints + ModuleInitializer)
    │   MinimalOpenApi.EndpointMapping.g.cs      (MapMinimalOpenApiEndpoints)
    │
    ▼
User project: inherit handler base, fill in business logic
    │
    ▼
Runtime startup
  builder.Services.AddMinimalOpenApi()      ← invokes generated AddGeneratedEndpoints
  app.MapMinimalOpenApiEndpoints()          ← maps all routes to handler lambdas
```

---

## 4. Package responsibilities

### 4.1 `MinimalOpenAPI` (meta-package)

NuGet entry point.  Has no C# code of its own.  Its sole purpose is to pull in:

- `MinimalOpenAPI.Runtime` — runtime DI and routing APIs.
- `MinimalOpenAPI.Generator` — Roslyn analyzer (source generator).

Both are declared as `ProjectReference` items in the csproj (the generator with
`ReferenceOutputAssembly="false"` so the meta-package does not link against the
generator DLL).  When the meta-package is packed, NuGet converts both references
into `<dependency>` entries in the `.nuspec`, so a consumer only needs:

```xml
<PackageReference Include="MinimalOpenAPI" Version="*" />
```

to receive the runtime *and* the generator automatically.  This is the same
pattern used by packages like `NetEscapades.EnumGenerators`.

It also ships `buildTransitive/MinimalOpenAPI.targets` which handles the MSBuild
plumbing described in §5.

### 4.2 `MinimalOpenAPI.Runtime`

Contains a single class, `ServiceCollectionExtensions`, with four methods:

- **`RegisterGeneratedServices(Action<IServiceCollection>)`** — called by the
  source-generated `[ModuleInitializer]` (§6.4) to register a callback that
  will wire in all generated handlers before the application starts.
- **`RegisterEndpointMapping(Func<IEndpointRouteBuilder, string?, RouteGroupBuilder>)`** —
  called by the same `[ModuleInitializer]` to register a callback that performs
  the actual endpoint mapping.
- **`AddMinimalOpenApi(IServiceCollection)`** — called by the application in
  `Program.cs`.  Invokes the `RegisterGeneratedServices` callback.
- **`MapMinimalOpenApiEndpoints(IEndpointRouteBuilder, string?)`** — called by
  the application in `Program.cs`.  Invokes the `RegisterEndpointMapping`
  callback.  Falls back to an empty route group if no generator has run.

The indirection through static callback fields is what lets the *generated*
code (which lives in a different conceptual layer from the runtime) hook into
two single user-facing API calls without requiring reflection or a `using` for
the generated namespace.

Because `MapMinimalOpenApiEndpoints` is defined in the `MinimalOpenAPI` runtime
namespace (the same one as `AddMinimalOpenApi`), users only need:

```csharp
using MinimalOpenAPI;
// ...
app.MapMinimalOpenApiEndpoints();
```

No `using {RootNamespace}.Generated;` is required.

### 4.3 `MinimalOpenAPI.Generator`

The Roslyn incremental source generator (§6).  Targets `netstandard2.0` as
required by the Roslyn analyzer host.

### 4.4 `MinimalOpenAPI.Abstractions`

Defines the object model and the parser abstraction:

- `OpenApiDocument` — top-level container (title, version, operations, schemas).
- `OpenApiOperation` — one path+method combination (operationId, route, HTTP
  method, parameters, request body, responses, summary, description, tags).
- `OpenApiParameter` — a single parameter with its location (`Path`, `Query`,
  `Header`, `Cookie`), name, schema, and required flag.
- `OpenApiRequestBody` — optional request body with its schema.
- `OpenApiResponse` — status code, description, and optional response schema.
- `OpenApiSchema` — a recursive type covering primitives, arrays (`type: array`
  with `items`), object schemas (with `properties` and `required`), `$ref`
  references, `nullable`, `format`.
- `IOpenApiParser` — `Task<OpenApiDocument> ParseAsync(string content, CancellationToken)`.

### 4.5 `MinimalOpenAPI.Parser.Yaml`

Implements `IOpenApiParser` using **YamlDotNet**.  Supports OpenAPI 3.x YAML
files.  The parser is stateless and can be instantiated once per file.

---

## 5. MSBuild integration

The `buildTransitive/MinimalOpenAPI.targets` file is responsible for the
compile-time plumbing.  It is imported automatically when the package is
restored (via `buildTransitive/`) and must also be imported explicitly when the
project references `MinimalOpenAPI` by project reference (development scenario).

Key targets:

| Target | Runs before | Purpose |
|--------|-------------|---------|
| `AddMinimalOpenApiFilesToAdditionalFiles` | `GenerateMSBuildEditorConfigFileCore`, `CoreCompile` | Copies `<OpenApi>` items into `<AdditionalFiles>` with `MinimalOpenApiFile=true` metadata and exposes `RootNamespace` as a compiler-visible property. |
| `AddMinimalOpenApiGeneratorDependencyAnalyzers` | `CoreCompile` | Adds `MinimalOpenAPI.Abstractions`, `MinimalOpenAPI.Parser.Yaml`, and `YamlDotNet` as `<Analyzer>` items so Roslyn can load them into its isolated `AssemblyLoadContext` alongside the generator DLL. |

The `<CompilerVisibleItemMetadata>` and `<CompilerVisibleProperty>` declarations
make `MinimalOpenApiFile` and `RootNamespace` readable via
`AnalyzerConfigOptionsProvider` inside the generator.

---

## 6. Source generator internals (`MinimalOpenAPI.Generator`)

The generator is an `IIncrementalGenerator`.  Its `Initialize` method sets up a
pipeline of incremental steps that Roslyn can cache and invalidate individually.

### 6.1 Collecting OpenAPI files

```csharp
context.AdditionalTextsProvider
    .Combine(context.AnalyzerConfigOptionsProvider)
    .Where(/* MinimalOpenApiFile == "true" */)
    .Select(/* read content + path */)
```

Only additional files explicitly tagged with `MinimalOpenApiFile=true` are
considered.  This avoids accidentally picking up other `AdditionalFiles` items.

### 6.2 Selecting a parser

`SelectParser(string path)` matches the file extension:

- `.yaml` / `.yml` → `YamlOpenApiParser`
- anything else → `null` (emits diagnostic **MOA005**)

The design is intentionally open for extension: adding JSON support means
adding a new `IOpenApiParser` implementation and a new case to `SelectParser`.

### 6.3 Discovering user implementations

The generator scans every `ClassDeclarationSyntax` in the user's project to
find concrete (non-abstract) classes that inherit from a generated base class.
For each operation it looks for:

- A class whose base-type chain includes `<OperationId>Endpoint` → the handler
  implementation.
- A class whose base-type chain includes `<OperationId>EndpointRegistration` →
  the optional customizer implementation.

Exactly one handler must exist (zero → **MOA001** warning; two or more →
**MOA002** error).  Zero or one customizer is allowed (two or more → **MOA003**
error).

### 6.4 Generated files

For a spec with `n` operations the generator emits `2n + 3` source files:

**Per-operation (emitted for every operation):**

| File | Purpose |
|------|---------|
| `MinimalOpenApi.<Id>Endpoint.g.cs` | Abstract handler base class with a `virtual HandleAsync(…)` method typed to the operation's parameters and `Results<T…>` return type. |
| `MinimalOpenApi.<Id>EndpointRegistration.g.cs` | Abstract customizer base class with a `virtual Configure(RouteHandlerBuilder)` method. |

**Shared (one per document):**

| File | Purpose |
|------|---------|
| `MinimalOpenApi.Dtos.g.cs` | One `sealed record` per `components/schemas` object. Properties are typed using `TypeMapper.MapSchema`. Required properties get non-nullable types with default values; optional properties get nullable types. |
| `MinimalOpenApi.DependencyInjection.g.cs` | `AddGeneratedEndpoints(IServiceCollection)` extension method that registers each handler (as `services.AddScoped<Base, Impl>()`) and each customizer (as `services.AddSingleton<Base, Impl>()`). Also contains `MinimalOpenApiModuleInitializer` which uses `[ModuleInitializer]` to call both `ServiceCollectionExtensions.RegisterGeneratedServices` AND `ServiceCollectionExtensions.RegisterEndpointMapping` the moment the assembly is loaded. |
| `MinimalOpenApi.EndpointMapping.g.cs` | **Internal** `MinimalOpenApiGeneratedEndpointRouteBuilderExtensions` class with an `internal static MapEndpoints(IEndpointRouteBuilder, string?)` method. Each route is mapped using `group.MapGet/Post/Put/…` with a static lambda that resolves the handler from DI, calls `handler.HandleAsync(…)`, and applies `.WithName`, `.WithSummary`, `.WithDescription`, `.WithTags`, and `.Produces<T>` metadata from the OpenAPI spec. The public entry point is the runtime's `MapMinimalOpenApiEndpoints`; this class is an internal implementation detail registered via the callback in §4.2. |

All generated types carry `[ExcludeFromCodeCoverage]` and `[GeneratedCode]`
attributes to prevent them from appearing in coverage reports.

### 6.5 Handler base class pattern

```csharp
// generated
public class GetTodoEndpoint
{
    public virtual Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException(…);
}

// user-written
public sealed class GetTodoHandler : GetTodoEndpoint
{
    public override Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        // business logic here
    }
}
```

The handler is a plain class registered with `AddScoped`.  ASP.NET Core's
minimal-API parameter binding injects it directly into the lambda, so no
middleware or action filter machinery is involved.

### 6.6 Registration customizer pattern (optional)

For every operation, the generator also emits a customizer base:

```csharp
// generated
public abstract class GetTodoEndpointRegistration
{
    public virtual void Configure(RouteHandlerBuilder builder) { }
}

// optional user-written
public sealed class GetTodoRegistration : GetTodoEndpointRegistration
{
    public override void Configure(RouteHandlerBuilder builder)
        => builder.RequireAuthorization();
}
```

If a concrete customizer is found, it is registered as a singleton and called
during `MapMinimalOpenApiEndpoints` to apply additional endpoint metadata.
If none exists, the customizer lookup is simply skipped.

---

## 7. Type mapping (`TypeMapper`)

`TypeMapper` converts OpenAPI concepts to C# identifiers and types.

| OpenAPI type + format | C# type |
|-----------------------|---------|
| `string` | `string` |
| `string` + `uuid` | `global::System.Guid` |
| `string` + `date-time` | `global::System.DateTimeOffset` |
| `integer` | `int` |
| `integer` + `int64` | `long` |
| `number` | `double` |
| `number` + `float` | `float` |
| `boolean` | `bool` |
| `array` | `<itemType>[]` |
| `$ref` | the referenced schema name (resolved to last path segment) |

**Nullability**: a type is made nullable (`?`) when the OpenAPI schema has
`nullable: true` or when the property is not listed in the parent schema's
`required` array.

**Route constraints**: path parameters are given inline ASP.NET Core type
constraints (e.g. `{id:guid}`, `{page:int}`) to ensure proper route matching
and automatic 400 responses for invalid values.

**Return type**: `BuildReturnType` collects all status codes from an operation's
responses, maps each to its `Microsoft.AspNetCore.Http.HttpResults.*` type, and
wraps multiple types in `Results<T1, T2, …>`.

**Naming conventions**:

- `ToPascalCase` — first letter uppercased.  Used for handler class names and
  DTO property names.
- `ToCamelCase` — first letter lowercased.  Used for C# parameter names in
  lambdas and `HandleAsync` signatures.
- `HandlerClassName(operationId)` → `<PascalCase(operationId)>Endpoint`
- `RegistrationClassName(operationId)` → `<PascalCase(operationId)>EndpointRegistration`

---

## 8. Diagnostics

| Code | Severity | Trigger |
|------|----------|---------|
| **MOA001** | Warning | No concrete class inheriting from a generated handler base was found in the project.  The app will compile but `HandleAsync` will throw `NotImplementedException` at runtime. |
| **MOA002** | Error | Two or more classes inherit from the same generated handler base.  Exactly one implementation is required. |
| **MOA003** | Error | Two or more classes inherit from the same generated customizer base.  At most one is allowed. |
| **MOA004** | Error | The OpenAPI file could not be parsed (YAML syntax error, etc.). |
| **MOA005** | Error | The `<OpenApi>` item has a file extension the generator does not recognise (only `.yaml`/`.yml` are supported). |

---

## 9. Runtime startup hook (ModuleInitializer)

The generated `MinimalOpenApiModuleInitializer` class uses the
`[System.Runtime.CompilerServices.ModuleInitializer]` attribute to call two
methods on `ServiceCollectionExtensions` the instant the consuming assembly is
loaded by the CLR — before any user code in `Program.cs` runs:

1. `RegisterGeneratedServices` — stores the DI registration callback.
2. `RegisterEndpointMapping` — stores the endpoint mapping callback.

This lets the user call only:

```csharp
builder.Services.AddMinimalOpenApi();
// …
app.MapMinimalOpenApiEndpoints();
```

without needing to reference any generated type directly and without needing a
`using {RootNamespace}.Generated;` import.  The generated code stores callbacks,
and the runtime methods invoke them.  The public `MapMinimalOpenApiEndpoints` is
defined entirely in `MinimalOpenAPI.Runtime` (namespace `MinimalOpenAPI`), just
like `AddMinimalOpenApi`.

---

## 10. Adding a new parser (extensibility point)

To support a new OpenAPI format (e.g. JSON):

1. Create a new project (e.g. `MinimalOpenAPI.Parser.Json`).
2. Reference `MinimalOpenAPI.Abstractions` and implement `IOpenApiParser`.
3. In `MinimalOpenApiGenerator.SelectParser`, add the new extension(s):

   ```csharp
   ".json" => new JsonOpenApiParser(),
   ```

4. Add the new DLL as an `<Analyzer>` in `MinimalOpenAPI.targets` so Roslyn can
   load it.

No changes to any other part of the generator are required.

---

## 11. Testing strategy

| Test project | What it tests |
|---|---|
| `MinimalOpenAPI.Generator.Tests` | Roslyn driver tests: a known `openapi.yaml` fixture is fed to the generator via `CSharpGeneratorDriver`; the emitted source is checked for expected class names, method signatures, and DI registrations. |
| `MinimalOpenAPI.Runtime.Tests` | Unit tests for `ServiceCollectionExtensions` (registration callback wiring). |
| `MinimalOpenAPI.IntegrationTests` | `WebApplicationFactory` tests that boot the sample Todo API end-to-end and make HTTP requests, verifying status codes and JSON payloads. |

The CI pipeline (`ci.yml`) builds with `--warnaserror` so no warning can be
silently introduced.

---

## 12. Key design decisions and trade-offs

**Generator targets `netstandard2.0`**: Required by the Roslyn analyzer host,
which imposes this TFM for all analyzers.  The rest of the stack targets
`net10.0`.

**`static` lambda in `MapXxx`**: The generated `group.MapGet(…, static (…) => …)`
uses a `static` lambda to avoid accidental closure captures.  The handler is
injected as a regular lambda parameter (ASP.NET Core's minimal-API DI binding).

**Handler as plain class, not interface**: Using a base class with a `virtual`
method allows the generator to provide a descriptive `NotImplementedException`
message (including the concrete type name and operation ID) when the developer
forgets to override `HandleAsync`.  An interface would throw a less helpful
`InvalidOperationException` from the DI container.

**`[ModuleInitializer]` for both DI wiring and endpoint mapping**: The
alternative would be requiring the developer to call
`services.AddGeneratedEndpoints()` and the generated `MapMinimalOpenApiEndpoints`
explicitly, leaking generated type names (and their namespaces) into user code.
The module initializer + callback pattern keeps the public API surface to two
calls — `AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()` — both in the
`MinimalOpenAPI` namespace, with no `using {RootNamespace}.Generated;` required.

**Customizer as singleton, handler as scoped**: Handlers may hold scoped
dependencies (e.g. `DbContext`).  Customizers configure route metadata at
startup and are stateless, so singleton lifetime is appropriate.

**One schema → one record**: All `components/schemas` objects are emitted as
`sealed record` types in a single `MinimalOpenApi.Dtos.g.cs` file.  Using
records gives value-based equality for free, which is useful in tests.
