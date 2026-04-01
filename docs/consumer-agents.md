# Consumer Agent Guide — MinimalOpenAPI

This guide is for coding agents (e.g. GitHub Copilot) integrating `MinimalOpenAPI` into a consumer project.

---

## 1. Purpose

`MinimalOpenAPI` is a **contract-first** OpenAPI framework for ASP.NET Core Minimal APIs.

- You author an OpenAPI YAML or JSON specification.
- A Roslyn source generator reads the spec at **build time** and emits DTO records, abstract handler base classes, DI registration, and endpoint mapping.
- The OpenAPI file is the **single source of truth**. The generated C# is a by-product.
- You only write business logic by extending the generated abstractions.

---

## 2. Package selection guide

| Package | When to reference it |
|---------|----------------------|
| `MinimalOpenAPI` | **Always.** This is the main entry point. It bundles the Roslyn source generator and pulls in `MinimalOpenAPI.Runtime`, `MinimalOpenAPI.Parser.Yaml`, and `MinimalOpenAPI.Parser.Json` as transitive dependencies. |
| `MinimalOpenAPI.Runtime` | Only if you need to reference runtime types (e.g. `IEndpointHandler`) in a project that does not reference the main package. Normally pulled in automatically. |
| `MinimalOpenAPI.Parser.Yaml` | Pulled in automatically. Reference directly only when writing or testing a custom generator host. |
| `MinimalOpenAPI.Parser.Json` | Pulled in automatically. Reference directly only when writing or testing a custom generator host. |
| `MinimalOpenAPI.Abstractions` | Only when writing a custom `IOpenApiParser` implementation in a separate project. |

**Rule:** reference only `MinimalOpenAPI` in your consumer project. Do not add the sub-packages separately unless there is a specific, justified reason.

---

## 3. Minimal setup (step-by-step)

**Step 1 — Install the package:**

```shell
dotnet add package MinimalOpenAPI
```

**Step 2 — Add your OpenAPI spec to the project directory:**

```
MyApi/
  openapi.yaml      ← your contract
  MyApi.csproj
  Program.cs
```

**Step 3 — Register the spec file in the `.csproj`:**

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0-alpha" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

**Step 4 — Build to generate the code:**

```shell
dotnet build
```

The generator emits code into the Roslyn compilation. The generated types are immediately available.

**Step 5 — Implement the generated handler base class for each operation:**

```csharp
// GetItemEndpoint.cs
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

**Step 6 — Register services and map endpoints in `Program.cs`:**

```csharp
using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
app.Run();
```

**Step 7 — Run the application:**

```shell
dotnet run
```

---

## 4. Example workflow

Given this OpenAPI spec fragment:

```yaml
paths:
  /todos:
    get:
      operationId: listTodos
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
components:
  schemas:
    Todo:
      type: object
      required: [id, title, isComplete]
      properties:
        id:
          type: string
          format: uuid
        title:
          type: string
        isComplete:
          type: boolean
```

The generator produces:

- `Todo` — a DTO record mirroring `components/schemas/Todo`.
- `ListTodosEndpointBase` — abstract base with a `Parameters` record wrapping query parameters.
- `CreateTodoEndpointBase` — abstract base with a nested `Request` record wrapping the request body.

**Consumer implementation for `listTodos`:**

```csharp
public sealed class ListTodosEndpoint(ITodoStore store) : ListTodosEndpointBase
{
    public override Task<Ok<Todo[]>> HandleAsync(
        ListTodosEndpointBase.Parameters parameters,
        CancellationToken cancellationToken)
    {
        var items = store.List(parameters.IsComplete)
                         .Select(t => new Todo { Id = t.Id, Title = t.Title, IsComplete = t.IsComplete })
                         .ToArray();
        return Task.FromResult(TypedResults.Ok(items));
    }
}
```

**Consumer implementation for `createTodo`:**

```csharp
public sealed class CreateTodoEndpoint(ITodoStore store) : CreateTodoEndpointBase
{
    public override Task<Results<Created<Todo>, BadRequest>> HandleAsync(
        Request request,
        CancellationToken cancellationToken)
    {
        var id = store.Add(request.Title);
        var todo = new Todo { Id = id, Title = request.Title, IsComplete = request.IsComplete };
        return Task.FromResult<Results<Created<Todo>, BadRequest>>(
            TypedResults.Created($"/todos/{id}", todo));
    }
}
```

Non-path parameters (query, header, cookie) are grouped into a nested `Parameters` record decorated with `[AsParameters]`. Path parameters are passed directly as method arguments. Request body schemas become a nested `Request` record. Inline response schemas become nested records named after the status code (e.g. `OkResponse`).

---

## 5. Key invariants

Agents **must not** violate these rules:

- **The OpenAPI spec is the source of truth.** All types and signatures originate from the spec. If the contract needs to change, update the spec, rebuild, and then update the implementation.
- **Do not manually edit generated files.** Generated code lives in the Roslyn compilation and is re-created on every build. Edits to generated output will be lost.
- **Extend generated abstractions.** Consumer logic goes into a concrete class that inherits from the generated `<OperationId>EndpointBase`. Do not implement `IEndpointHandler` directly or write manual `app.Map*` calls unless generated code is deliberately bypassed for a justified reason.
- **Do not bypass generation with manual endpoint wiring** unless the generator does not support a specific OpenAPI feature and there is no alternative.
- **One concrete handler per operation.** The generator enforces this: multiple implementations of the same base class cause build diagnostic `MOA002`.
- **Do not call `base.HandleAsync(…)`.** The base implementation throws `NotImplementedException` by design.

---

## 6. Non-goals and limitations

- **OpenAPI 3.0.x only.** OpenAPI 3.1.x and Swagger 2.0 are not supported.
- **No runtime OpenAPI document serving.** The framework does not expose a `/openapi.json` endpoint or integrate with Swashbuckle/Scalar.
- **No schema composition.** `allOf`, `oneOf`, and `anyOf` are not supported. Use `$ref` or inline object schemas.
- **Single spec file per project.** Multiple `<OpenApi>` items may conflict when operation IDs or schema names clash.
- **No runtime request validation.** Model binding uses ASP.NET Core defaults. OpenAPI-level constraints (pattern, min/max) are not enforced at runtime.
- **No code-first path.** If you need to generate an OpenAPI document from C# code, use Swashbuckle, NSwag, or `Microsoft.AspNetCore.OpenApi` instead.
- **Requires .NET 10** at runtime.

---

## 7. Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Generated types not found after adding the spec | `<OpenApi>` item missing or path is wrong | Add `<OpenApi Include="openapi.yaml" />` to the `.csproj` and rebuild. |
| `MOA001` build warning | No concrete handler found for a generated base class | Create a class that inherits from `<OperationId>EndpointBase` and overrides `HandleAsync`. |
| `MOA002` build error | More than one class inherits from the same base | Remove or consolidate duplicate handler classes. |
| `MOA004` build error | Spec file could not be parsed | Check for YAML/JSON syntax errors. Validate with [Swagger Editor](https://editor.swagger.io). |
| `MOA005` build error | Unrecognised file extension | Use `.yaml`, `.yml`, or `.json`. |
| `MapMinimalOpenApiEndpoints` registers no routes | Generator did not run (missing `<OpenApi>` item or wrong path) | Verify the item group and rebuild. |
| `NotImplementedException` at runtime | Handler does not override `HandleAsync` or calls `base.HandleAsync(…)` | Override the method and remove the `base` call. |
| Spec and implementation out of sync | Spec was updated but code was not regenerated | Rebuild the project; the generator always re-runs on build. |

---

## 8. Integrating into an existing project

When adding `MinimalOpenAPI` to an existing ASP.NET Core Minimal API project:

1. **Inspect the existing project structure** before making changes. Understand which endpoints exist, how they are registered, and whether a parallel framework (e.g. controllers, other code-gen tools) is already in use.
2. **Prefer adding an OpenAPI contract** rather than rewriting existing endpoints. Define the spec for new endpoints; migrate existing endpoints to the contract-first model only if explicitly requested.
3. **Follow existing patterns.** If the project already has handler classes in a specific folder, place new handlers there.
4. **Do not introduce conflicting frameworks.** Do not register the same route with both `app.MapGet(…)` and `MapMinimalOpenApiEndpoints()`. This causes ambiguous routing.
5. **Register services exactly once.** Call `AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()` only once in `Program.cs`.

---

## 9. Do not do this

- **Do not edit files under `obj/` or any file marked `// <auto-generated>`** — changes will be overwritten on the next build.
- **Do not duplicate logic already generated.** The framework produces DI registration and endpoint mapping automatically; writing it by hand creates conflicts.
- **Do not ignore the OpenAPI contract.** Adding properties to DTO records or changing method signatures manually breaks the contract-first model and will be overwritten on rebuild.
- **Do not add `app.Map*` calls for operations already covered by the generator.** This registers duplicate routes.
- **Do not reference `MinimalOpenAPI.Runtime`, `MinimalOpenAPI.Parser.Yaml`, or `MinimalOpenAPI.Parser.Json` directly** unless you have a specific reason — they are already pulled in as transitive dependencies of `MinimalOpenAPI`.
