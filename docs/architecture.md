# MinimalOpenAPI — Architecture & Design

> **Audience**: human contributors and AI agents working on this repository.
> This document is the authoritative reference for how the library works, why it
> was designed the way it was, and how to extend it.

---

## 1. Intent

MinimalOpenAPI is a **contract-first** code-generation framework for ASP.NET Core
Minimal APIs.

The central idea is that the OpenAPI (YAML or JSON) file is the single source of truth.
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
| Source of truth | C# code | `openapi.yaml` / `openapi.json` |
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
  MinimalOpenAPI/               ← NuGet package: Roslyn source generator + runtime services
  MinimalOpenAPI.Abstractions/  ← OpenAPI document model + IOpenApiParser (internal, not published)
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser (internal, bundled in the package)
  MinimalOpenAPI.Parser.Json/   ← JSON parser (internal, bundled in the package)
sample/
  MinimalOpenAPI.Sample.Api/    ← end-to-end working example (Todo CRUD)
  MinimalOpenAPI.SmokeTest.Api/ ← minimal consumer that builds against the packed NuGet artifact
tests/
  MinimalOpenAPI.Generator.Tests/   ← generator unit tests (Roslyn driver)
  MinimalOpenAPI.Runtime.Tests/     ← runtime unit tests
  MinimalOpenAPI.IntegrationTests/  ← WebApplicationFactory integration tests
```

> **Package names vs directory names.**  The project in `src/MinimalOpenAPI/` is
> packed as **`MinimalOpenAPI`** (the `PackageId` / assembly name matches the `.csproj`
> filename `MinimalOpenAPI.csproj`).  The directory retains its original name for
> historical clarity.  There is no separate meta-package.

---

## 3. End-to-end data flow

```
openapi.yaml / openapi.json  (one or more <OpenApi> items)
    │
    │  MSBuild target AddMinimalOpenApiFilesToAdditionalFiles
    │  promotes <OpenApi> items → AdditionalFiles with
    │  build_metadata.AdditionalFiles.MinimalOpenApiFile = true
    │  (SpecName metadata carries the PascalCase sub-namespace segment)
    │
    ▼
MinimalOpenAPI.Generator  (IIncrementalGenerator, runs inside Roslyn)
    │
    ├─ Reads AdditionalFiles tagged with MinimalOpenApiFile = true
    ├─ Peeks openapi version field; selects parser via OpenApiParserRequest(Format, Version?)
    ├─ Calls IOpenApiParser.ParseAsync → OpenApiDocument
    ├─ Scans user-project class declarations (SyntaxProvider)
    │   to discover concrete handler and customizer implementations
    │
    ├─ Emits per-operation source files (prefixed with spec name):
    │   MinimalOpenApi.<SpecName>.<OperationId>Endpoint.g.cs          (handler base)
    │   MinimalOpenApi.<SpecName>.<OperationId>EndpointRegistration.g.cs (customizer base)
    │
    ├─ Emits shared source files (one set per spec):
    │   MinimalOpenApi.<SpecName>.Dtos.g.cs                 (DTO records + enums)
    │   MinimalOpenApi.<SpecName>.DependencyInjection.g.cs  (AddGeneratedEndpoints + ModuleInitializer)
    │   MinimalOpenApi.<SpecName>.EndpointMapping.g.cs      (MapEndpoints internal helper)
    │
    ▼
User project: inherit handler base, fill in business logic
    │
    ▼
Runtime startup
  builder.Services.AddMinimalOpenApi()      ← invokes generated AddGeneratedEndpoints
  app.MapMinimalOpenApiEndpoints()          ← maps all routes to handler lambdas
  app.MapOpenApiSchemas()                   ← serves spec files as GET endpoints (optional)
```

---

## 4. Package responsibilities

### 4.1 `MinimalOpenAPI`

The single published NuGet package.  The project lives in `src/MinimalOpenAPI/`
and is packed as the **`MinimalOpenAPI`** package.

The project is multi-targeted:

- `netstandard2.0` — builds the Roslyn analyzer DLL (required by the Roslyn host).
  All parser and abstractions DLLs are bundled here under `analyzers/dotnet/cs/`
  alongside the generator.
- `net10.0` — builds the runtime DLL (`lib/net10.0/MinimalOpenAPI.dll`) which
  contains the ASP.NET Core extension methods (`AddMinimalOpenApi`,
  `MapMinimalOpenApiEndpoints`, `MapOpenApiSchemas`) and supporting types.

A consumer only needs:

```xml
<PackageReference Include="MinimalOpenAPI" Version="*" />
```

Because `<DevelopmentDependency>true</DevelopmentDependency>` is set, NuGet
automatically adds `PrivateAssets="all"` when consumers install the package,
preventing it from leaking into their transitive dependency closure.

The package ships `buildTransitive/MinimalOpenAPI.targets` which handles the
MSBuild plumbing described in §5.

#### Runtime services (`ServiceCollectionExtensions`)

The `net10.0` DLL contains a single class, `ServiceCollectionExtensions`, with
six public methods (in namespace `MinimalOpenAPI`):

- **`RegisterGeneratedServices(Action<IServiceCollection>)`** — called once per spec by the
  source-generated `[ModuleInitializer]` (§6.4) to register a callback that
  will wire in all generated handlers before the application starts.
- **`RegisterEndpointMapping(Action<IEndpointRouteBuilder, RouteGroupBuilder>)`** —
  called once per spec by the same `[ModuleInitializer]` to register a callback that
  maps routes onto the shared `RouteGroupBuilder` created by `MapMinimalOpenApiEndpoints`.
- **`AddMinimalOpenApi(IServiceCollection)`** — called by the application in
  `Program.cs`.  Invokes all registered `RegisterGeneratedServices` callbacks.
- **`MapMinimalOpenApiEndpoints(IEndpointRouteBuilder, string?)`** — called by
  the application in `Program.cs`.  Creates a single `RouteGroupBuilder` and invokes
  all registered `RegisterEndpointMapping` callbacks with it.  Falls back gracefully
  if no generator has run.
- **`MapOpenApiSchemas(IEndpointRouteBuilder, string? prefix, string? schemasDirectory)`** —
  maps schema endpoints from generator-registered metadata. Each schema file is
  copied internally under `openapi/schemas/<SchemaId>/<filename>.<ext>`, and only
  items with `PublishAs` are exposed over HTTP. The public path is exactly the
  configured `PublishAs` value, while descriptor name/version come from
  `DisplayName` / `DisplayVersion` metadata with runtime fallbacks.
- **`ResetForTesting()`** — clears all registered callbacks; intended for use in
  unit tests that exercise `ServiceCollectionExtensions` in isolation.

The indirection through static callback lists is what lets the *generated*
code (which lives in a different conceptual layer from the runtime) hook into
two single user-facing API calls without requiring reflection or a `using` for
the generated namespace.

### 4.2 `MinimalOpenAPI.Abstractions` (internal, not published)

Defines the object model and the parser abstraction used internally by the
generator and parsers.  Not published as a separate NuGet package; its DLL is
bundled under `analyzers/dotnet/cs/` in the `MinimalOpenAPI` package.

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
- `IOpenApiParser` — `bool CanParse(OpenApiParserRequest request)` to declare which format and version range a parser handles, and `Task<OpenApiDocument> ParseAsync(string content, CancellationToken)` to perform the parse.

### 4.3 `MinimalOpenAPI.Parser.Yaml` (internal, not published)

Implements `IOpenApiParser` using **YamlDotNet**.  Supports OpenAPI 3.x YAML
files.  The parser is stateless and can be instantiated once per file.
Not published as a separate NuGet package; bundled under `analyzers/dotnet/cs/`.

### 4.4 `MinimalOpenAPI.Parser.Json` (internal, not published)

Implements `IOpenApiParser` using **System.Text.Json**.  Supports OpenAPI 3.x JSON
files.  The parser is stateless and can be instantiated once per file.
Not published as a separate NuGet package; bundled under `analyzers/dotnet/cs/`.

---

## 5. MSBuild integration

The `buildTransitive/MinimalOpenAPI.targets` file is responsible for the
compile-time plumbing.  It is imported automatically when the package is
restored (via `buildTransitive/`) and must also be imported explicitly when the
project references `MinimalOpenAPI` by project reference (development scenario).

Key targets:

| Target | Hook | Purpose |
|--------|------|---------|
| `AddMinimalOpenApiFilesToAdditionalFiles` | `BeforeTargets="GenerateMSBuildEditorConfigFileCore;CoreCompile"` | Copies `<OpenApi>` items into `<AdditionalFiles>` with `MinimalOpenApiFile=true` metadata and exposes `RootNamespace` as a compiler-visible property. |
| `AddMinimalOpenApiGeneratorDependencyAnalyzers` | `BeforeTargets="CoreCompile"` | Adds `MinimalOpenAPI.Abstractions`, `MinimalOpenAPI.Parser.Yaml`, `MinimalOpenAPI.Parser.Json`, and `YamlDotNet` as `<Analyzer>` items so Roslyn can load them into its isolated `AssemblyLoadContext` alongside the generator DLL. |
| `CopyMinimalOpenApiFilesToOutput` | `AfterTargets="Build"` | Copies every `<OpenApi ... />` file to `$(OutDir)openapi/schemas/<SchemaId>/<filename>.<ext>`, preserving collision-safe hashed internal storage. |
| `AddMinimalOpenApiFilesToPublishOutput` | `AfterTargets="ComputeFilesToPublish"` | Injects `ResolvedFileToPublish` items for every `<OpenApi ... />` file so `dotnet publish` includes the same hashed internal path structure. |

The `<CompilerVisibleItemMetadata>` and `<CompilerVisibleProperty>` declarations
make `MinimalOpenApiFile` and `RootNamespace` readable via
`AnalyzerConfigOptionsProvider` inside the generator.

### 5.1 Contract-package pattern (gRPC-style)

An OpenAPI spec can be shipped inside a separate "contracts" NuGet package and
consumed transparently — the same pattern used by gRPC `.proto` files.

**In the contracts package** (`build/MyContracts.targets` or
`buildTransitive/MyContracts.targets`):

```xml
<ItemGroup>
  <!-- Path resolves relative to this targets file inside the NuGet cache. -->
  <OpenApi Include="$(MSBuildThisFileDirectory)../content/api.yaml"
           PublishAs="/openapi/schema.yaml"
           DisplayName="Contracts API" />
</ItemGroup>
```

**In the consuming project** — no `<OpenApi>` declaration needed at all; the
package contributes the item automatically on restore.  If the consuming project
wants to opt into publishing without modifying the contracts package, it can
update the metadata inside a target so the update runs after all static items
from NuGet packages have been evaluated:

```xml
<Target Name="PublishAllOpenApiSpecsAtExplicitPaths"
        BeforeTargets="CopyMinimalOpenApiFilesToOutput">
  <ItemGroup>
    <OpenApi Update="@(OpenApi)"
             PublishAs="/openapi/contracts/schema.yaml"
             DisplayName="Contracts API" />
  </ItemGroup>
</Target>
```

The `CopyMinimalOpenApiFilesToOutput` and `AddMinimalOpenApiFilesToPublishOutput`
targets process all `<OpenApi ... />` items regardless of origin
(project file, contracts package, or any other imported `.targets` file).  File
content is preserved byte-for-byte; YAML stays YAML, JSON stays JSON.

**Serving the spec via HTTP** — call `MapOpenApiSchemas()` in `Program.cs`. It
maps endpoints for registered schemas that declare `PublishAs`:

```csharp
using MinimalOpenAPI;

app.MapMinimalOpenApiEndpoints();
app.MapOpenApiSchemas();  // serves GET /openapi/schema.yaml (or your PublishAs path)
```

The method returns descriptors that can be used for Swagger UI wiring:

```csharp
var schemas = app.MapOpenApiSchemas();
foreach (var schema in schemas.Schemas)
{
    options.SwaggerEndpoint(schema.PublicPath, schema.Name);
}
```

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

`SelectParser(string path, string content)` builds an `OpenApiParserRequest` by
detecting the format and peeking the version, then iterates `_parsers` to find
the first parser whose `CanParse` returns `true`:

**Format detection** — from the file extension:

| Extension | `OpenApiFormat` |
|-----------|-----------------|
| `.yaml` / `.yml` | `Yaml` |
| `.json` | `Json` |
| anything else | `Unknown` |

**Version peek** — a lightweight regex scans the raw content for the top-level
`openapi` field (no full parse).  The result is a `Version?` (`null` when the
field is absent or unparseable).

Both values are bundled into an `OpenApiParserRequest(Format, Version?)` and
passed to each parser's `CanParse`.  The current parsers only check the format:

```csharp
// YamlOpenApiParser
public bool CanParse(OpenApiParserRequest request) => request.Format == OpenApiFormat.Yaml;

// JsonOpenApiParser
public bool CanParse(OpenApiParserRequest request) => request.Format == OpenApiFormat.Json;
```

A future version-targeted parser can additionally filter on the version:

```csharp
// Hypothetical parser for a future breaking-change version
public bool CanParse(OpenApiParserRequest request) =>
    request.Format == OpenApiFormat.Yaml && request.Version?.Major == 4;
```

If no parser returns `true`, `SelectParser` returns `null` and the generator
emits diagnostic **MOA005**.

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
public class GetTodoEndpointBase
{
    public virtual Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException(…);
}

// user-written
public sealed class GetTodoEndpoint : GetTodoEndpointBase
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
| `string` + `date` | `global::System.DateOnly` |
| `integer` | `int` |
| `integer` + `int64` | `long` |
| `number` | `double` |
| `number` + `float` | `float` |
| `boolean` | `bool` |
| `array` | `<itemType>[]` |
| schema with `enum` values | C# `enum` decorated with `[JsonConverter(typeof(JsonStringEnumConverter))]` |
| `$ref` | the referenced schema name, fully qualified with the contracts namespace |
| `object` + `additionalProperties: { primitive/array schema }` | `global::System.Collections.Generic.Dictionary<string, T>` where `T` is the mapped value type |
| `object` + `additionalProperties: { inline object schema }` | `global::System.Collections.Generic.Dictionary<string, TValue>` where `TValue` is a generated record (see below) |
| `object` + `additionalProperties: true` + `properties: …` | record with named properties + `[JsonExtensionData] Dictionary<string, JsonElement>? Extensions` |

**`enum` schemas**

A schema with an `enum` list generates a C# `enum` decorated with
`[JsonConverter(typeof(JsonStringEnumConverter))]`.  Top-level component schemas
produce a named enum in the `Contracts` namespace.  Inline enum schemas on object
properties produce a derived enum named `{ContainingSchema}{PascalCase(PropertyName)}`
(e.g. `Product.category` → `ProductCategory`).

`[ExcludeFromCodeCoverage]` is intentionally not placed on `enum` declarations
because C# only allows that attribute on `class`, `struct`, `constructor`,
`method`, `property`, `indexer`, and `event` declarations.  Only
`[GeneratedCode]` is emitted before enum declarations.

**`additionalProperties` with an inline object value type**

When `additionalProperties` is itself an inline object schema the generator emits a
dedicated named record for the value type:

- **Component schemas** — the value record is emitted as a top-level type in the
  `Contracts` namespace and named `{SchemaName}{PropertyName}Value`.
  For example, `additionalProperties` on `Todo.metadata` produces a
  `TodoMetadataValue` record and types the property as
  `Dictionary<string, TodoMetadataValue>`.

- **Inline request / response schemas** — the value record is emitted as a
  sibling nested type inside the handler base class and named
  `{InlineTypeName}{PropertyName}Value`.
  For example, a `labels` dict property on an inline `Request` schema produces a
  `RequestLabelsValue` nested record and types the property as
  `Dictionary<string, RequestLabelsValue>`.

Value records are always emitted *before* the record that references them so that
the generated file is a valid single-pass compilation unit.

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

- `ToPascalCase` — handles snake_case, kebab-case, camelCase, and PascalCase inputs.
  Used for handler class names, DTO property names, nested record names, and enum names.
- `ToCamelCase` — first letter lowercased.  Used for C# parameter names in
  lambdas and `HandleAsync` signatures.
- `HandlerClassName(operationId)` → `<PascalCase(operationId)>EndpointBase`
- `RegistrationClassName(operationId)` → `<PascalCase(operationId)>EndpointRegistration`

---

## 8. Diagnostics

| Code | Severity | Trigger |
|------|----------|---------|
| **MOA001** | Warning | No concrete class inheriting from a generated handler base was found in the project.  The app will compile but `HandleAsync` will throw `NotImplementedException` at runtime. |
| **MOA002** | Error | Two or more classes inherit from the same generated handler base.  Exactly one implementation is required. |
| **MOA003** | Error | Two or more classes inherit from the same generated customizer base.  At most one is allowed. |
| **MOA004** | Error | The OpenAPI file could not be parsed (YAML syntax error, etc.). |
| **MOA005** | Error | The `<OpenApi>` item has a file extension the generator does not recognise (only `.yaml`, `.yml`, and `.json` are supported). |
| **MOA006** | Warning | The `openapi` version field is absent or contains a value the generator cannot parse as a `System.Version`.  The spec is still processed; generation continues. |

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
defined in the `MinimalOpenAPI` namespace (inside `lib/net10.0/MinimalOpenAPI.dll`),
just like `AddMinimalOpenApi`.

---

## 10. Adding a new parser

New parsers are added directly to this repository — they are not an external
extensibility point.  Because parser DLLs are bundled inside `analyzers/dotnet/cs/`
in the package, a third-party parser cannot be injected without repackaging.

### 10.1 New format (e.g. TOML)

To support a new OpenAPI file format:

1. Create a new project (e.g. `MinimalOpenAPI.Parser.Toml`) inside `src/`.
2. Reference `MinimalOpenAPI.Abstractions` and implement both methods of `IOpenApiParser`:
   - `CanParse(OpenApiParserRequest request)` — return `true` for `request.Format == OpenApiFormat.Toml`
     (after also adding `Toml` to the `OpenApiFormat` enum and the `DetectFormat` switch).
   - `ParseAsync(content)` — parse and return an `OpenApiDocument`.
3. Add the new parser instance to `_parsers` in `MinimalOpenApiGenerator`.
4. Add the new DLL as an `<Analyzer>` in `MinimalOpenAPI.targets` so Roslyn can load it.
5. Add the new `ProjectReference` (with `PrivateAssets="all"`) to `MinimalOpenAPI.csproj`.
6. Set `<IsPackable>false</IsPackable>` in the new project — it is bundled, not published separately.

### 10.2 Version-specific parser (e.g. OpenAPI 4.0)

When a new major version introduces breaking structural changes that cannot be
handled by adding branches to an existing parser:

1. Implement `IOpenApiParser` (in a new project or in an existing one):
   - `CanParse(OpenApiParserRequest request)` — check format **and** version:
     ```csharp
     return request.Format == OpenApiFormat.Yaml && request.Version?.Major == 4;
     ```
     Because the call site has already peeked the version from the raw content, this
     check is a simple, declarative expression — no string scanning inside the parser.
   - `ParseAsync(content)` — implement parsing for the new version's structure.
2. Prepend the new parser to `_parsers` in `MinimalOpenApiGenerator` so it wins
   selection for its target version before the existing catch-all parsers are tried.
   Existing parsers are unmodified.
3. Add the new DLL as an `<Analyzer>` in `MinimalOpenAPI.targets`.

The "can/do" selection model keeps each parser focused on a single version range
and avoids version-branching inside its parsing logic.

---

## 11. Testing strategy

| Test project | What it tests |
|---|---|
| `MinimalOpenAPI.Generator.Tests` | Roslyn driver tests: known `openapi.yaml` and `openapi.json` fixtures are fed to the generator via `CSharpGeneratorDriver`; the emitted source is checked for expected class names, method signatures, and DI registrations. |
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
