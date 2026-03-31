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

- **DTO records** — one `sealed record` per `components/schemas` object.
- **Handler base classes** — one abstract `<OperationId>EndpointBase` per operation with a strongly-typed `HandleAsync` signature.
- **DI registration** — a generated `AddGeneratedEndpoints` extension and a `[ModuleInitializer]` that wires everything up automatically.
- **Endpoint mapping** — a generated `MapEndpoints` that registers all routes.

You only write the business logic.

---

## Runtime requirements

- **.NET 10** is required at runtime. The `MinimalOpenAPI` package targets `net10.0` for its runtime dependency (`MinimalOpenAPI.Runtime`) and `netstandard2.0` for the Roslyn analyzer host.
- **ASP.NET Core** (via `Microsoft.AspNetCore.App` framework reference) is required in the consuming project.

---

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [`MinimalOpenAPI`](src/MinimalOpenAPI) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI)](https://www.nuget.org/packages/MinimalOpenAPI) | **Start here.** Bundles the Roslyn source generator and declares `MinimalOpenAPI.Runtime` as a dependency. One reference is all you need. |
| [`MinimalOpenAPI.Runtime`](src/MinimalOpenAPI.Runtime) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Runtime)](https://www.nuget.org/packages/MinimalOpenAPI.Runtime) | ASP.NET Core runtime services: `AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()`. Pulled in automatically by the main package. |
| [`MinimalOpenAPI.Abstractions`](src/MinimalOpenAPI.Abstractions) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Abstractions)](https://www.nuget.org/packages/MinimalOpenAPI.Abstractions) | OpenAPI document model (`OpenApiDocument`, `OpenApiOperation`, …) and the `IOpenApiParser` interface. Useful when writing a custom parser. |
| [`MinimalOpenAPI.Parser.Yaml`](src/MinimalOpenAPI.Parser.Yaml) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Parser.Yaml)](https://www.nuget.org/packages/MinimalOpenAPI.Parser.Yaml) | YAML OpenAPI spec parser, built on YamlDotNet. Included automatically via the main package. |
| [`MinimalOpenAPI.Parser.Json`](src/MinimalOpenAPI.Parser.Json) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Parser.Json)](https://www.nuget.org/packages/MinimalOpenAPI.Parser.Json) | JSON OpenAPI spec parser, built on `System.Text.Json`. Included automatically via the main package. |

Most consumers only need the top-level `MinimalOpenAPI` package. The remaining packages are split out for composability and are pulled in transitively.

### Pre-release packages

Pre-release packages are published to the GitHub Packages NuGet feed on manual execution of the Publish workflow:

```
https://nuget.pkg.github.com/Kralizek/index.json
```

---

## Installation

Add the `MinimalOpenAPI` package to your ASP.NET Core project:

```shell
dotnet add package MinimalOpenAPI
```

Or manually in your `.csproj`:

```xml
<PackageReference Include="MinimalOpenAPI" Version="1.0.0-alpha" />
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

### Inline request and response schemas

When a request body or response schema is defined inline (without a `$ref`), the generator emits a nested record inside the handler base class instead of a top-level DTO. The record name is derived from context: `Request` for request bodies, and a status-code-based name for responses (`OkResponse`, `CreatedResponse`, etc.).

For example, given this inline response spec:

```yaml
paths:
  /items/count:
    get:
      operationId: getItemCount
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: object
                required: [count]
                properties:
                  count:
                    type: integer
```

The generator produces `GetItemCountEndpointBase.OkResponse`. The handler uses it directly:

```csharp
// GetItemCountEndpoint.cs
public sealed class GetItemCountEndpoint(IItemRepository repo) : GetItemCountEndpointBase
{
    public override async Task<Ok<OkResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var count = await repo.CountAsync(cancellationToken);
        return TypedResults.Ok(new OkResponse { Count = count });
    }
}
```

---

### Realistic example — query parameters, path parameters, and request body

The following example is based on the bundled [sample app](sample/MinimalOpenAPI.Sample.Api) (a simple Todo CRUD API).

**`openapi.yaml` (relevant excerpts):**

```yaml
paths:
  /todos:
    get:
      operationId: listTodos
      summary: List all todos
      parameters:
        - name: isComplete
          in: query
          required: false
          schema:
            type: boolean
      responses:
        "200":
          description: OK
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Todo'
    post:
      operationId: createTodo
      summary: Create a todo
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [title, isComplete]
              properties:
                title:
                  type: string
                description:
                  type: string
                  nullable: true
                isComplete:
                  type: boolean
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Todo'
        "400":
          description: Bad Request
  /todos/{id}:
    get:
      operationId: getTodo
      summary: Get a todo by ID
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
                $ref: '#/components/schemas/Todo'
        "404":
          description: Not found
```

**Handler for `listTodos` — query parameter via `Parameters` record:**

Non-path parameters (query, header, cookie) are grouped into a nested `Parameters` record decorated with `[AsParameters]`, keeping handler signatures clean.

```csharp
// ListTodosEndpoint.cs
public sealed class ListTodosEndpoint(ITodoStore store) : ListTodosEndpointBase
{
    public override Task<Ok<Todo[]>> HandleAsync(
        ListTodosEndpointBase.Parameters parameters,  // generated; wraps isComplete
        CancellationToken cancellationToken)
    {
        var items = store.List(parameters.IsComplete)
                         .Select(t => new Todo { Id = t.Id, Title = t.Title, IsComplete = t.IsComplete })
                         .ToArray();

        return Task.FromResult(TypedResults.Ok(items));
    }
}
```

**Handler for `createTodo` — request body:**

```csharp
// CreateTodoEndpoint.cs
public sealed class CreateTodoEndpoint(ITodoStore store) : CreateTodoEndpointBase
{
    public override Task<Results<Created<Todo>, BadRequest>> HandleAsync(
        Request request,        // generated DTO — mirrors the requestBody schema
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Task.FromResult<Results<Created<Todo>, BadRequest>>(TypedResults.BadRequest());

        var id = store.Add(request.Title, request.Description);
        var todo = new Todo { Id = id, Title = request.Title, IsComplete = false };

        return Task.FromResult<Results<Created<Todo>, BadRequest>>(
            TypedResults.Created($"/todos/{id}", todo));
    }
}
```

**Handler for `getTodo` — path parameter:**

```csharp
// GetTodoEndpoint.cs
public sealed class GetTodoEndpoint(ITodoStore store) : GetTodoEndpointBase
{
    public override Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        Guid id,                // path parameter; type inferred from schema format: uuid
        CancellationToken cancellationToken)
    {
        var item = store.Get(id);
        return item is null
            ? Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.NotFound())
            : Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.Ok(item));
    }
}
```

---

## Limitations and non-goals

- **OpenAPI 3.0.x only.** OpenAPI 3.1.x support may be added in a future release. OpenAPI 2.0 (Swagger) is not supported.
- **No runtime OpenAPI document serving.** MinimalOpenAPI does not expose a `/openapi.json` endpoint or integrate with Swashbuckle/Scalar. It generates code from the spec; serving the spec is a separate concern.
- **Schema composition not supported.** Keywords such as `allOf`, `oneOf`, and `anyOf` are not yet supported. Both `$ref` schemas and inline object schemas (for request bodies and responses) are fully supported.
- **Single spec file per project.** Each `<OpenApi>` item produces generated code in the same namespace. Multiple spec files may conflict if they define the same operation IDs or schema names.
- **No runtime validation.** The generated code uses ASP.NET Core's built-in model binding. It does not perform OpenAPI-level request validation (e.g. pattern, min/max constraints).
- **No code-first path.** If you start from C# and want to generate an OpenAPI document, use Swashbuckle, NSwag, or Microsoft.AspNetCore.OpenApi instead.

---

## Troubleshooting

**Build error MOA001 — no handler implementation found**

The generator emits a warning when it cannot find a class that inherits from a generated `<OperationId>EndpointBase`. Add a concrete handler class:

```csharp
public sealed class GetItemEndpoint : GetItemEndpointBase
{
    public override async Task<Results<Ok<Item>, NotFound>> HandleAsync(
        Guid id, CancellationToken cancellationToken)
        => TypedResults.NotFound();
}
```

**Build error MOA002 — multiple handler implementations**

Only one class may inherit from a given generated base. Remove or consolidate the duplicate.

**Build error MOA004 — OpenAPI file could not be parsed**

Check the spec file for YAML/JSON syntax errors. Validate it with a tool like the [Swagger Editor](https://editor.swagger.io) before referencing it in the project.

**Build error MOA005 — unrecognised file extension**

Only `.yaml`, `.yml`, and `.json` are supported. Rename the file or use the correct extension in the `<OpenApi>` item.

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
  MinimalOpenAPI/               ← MinimalOpenAPI NuGet package (generator + runtime dep)
  MinimalOpenAPI.Runtime/       ← runtime services
  MinimalOpenAPI.Abstractions/  ← document model & parser contracts
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser implementation
  MinimalOpenAPI.Parser.Json/   ← JSON parser implementation
sample/
  MinimalOpenAPI.Sample.Api/    ← end-to-end Todo CRUD example
tests/
  MinimalOpenAPI.Generator.Tests/
  MinimalOpenAPI.Runtime.Tests/
  MinimalOpenAPI.IntegrationTests/
docs/
  architecture.md               ← internals, design decisions, extensibility
  releasing.md                  ← versioning and release process
```

For a deep-dive into the design, architecture, and internals see [docs/architecture.md](docs/architecture.md).

---

## Contributing

Contributions are welcome. Please open an issue first to discuss proposed changes.

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-change`).
3. Commit your changes — the CI pipeline enforces a warning-free build (`--warnaserror`) and runs all unit and integration tests.
4. Open a pull request against `master`.

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup details.
