# Getting started with MinimalOpenAPI

MinimalOpenAPI is a contract-first OpenAPI framework for ASP.NET Core Minimal APIs. You author the API contract first, then implement the strongly typed handler base classes generated from it.

## Prerequisites

- .NET 10 SDK
- An ASP.NET Core project
- An OpenAPI 3.0 or 3.1 document in YAML or JSON

## 1. Install the package

```shell
dotnet add package MinimalOpenAPI --version 1.0.0
```

`MinimalOpenAPI` is the only package you need. It contains the source generator and the required ASP.NET Core runtime services.

## 2. Add an OpenAPI document

Create `openapi.yaml` in the project directory:

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

Register the document in the project file:

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

## 3. Register MinimalOpenAPI

Add the generated services and endpoint mappings in `Program.cs`:

```csharp
using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

app.Run();
```

## 4. Build the project

```shell
dotnet build
```

The source generator reads the OpenAPI document and adds the contracts, handler base classes, dependency-injection registration, and endpoint mappings to the compilation.

## 5. Implement the generated handler

The `getItem` operation generates `GetItemEndpointBase`. Add a concrete implementation:

```csharp
using Microsoft.AspNetCore.Http.HttpResults;

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

MinimalOpenAPI discovers the implementation through source generation. No manual handler registration or `MapGet()` call is required.

Register any application dependencies normally:

```csharp
builder.Services.AddScoped<IItemRepository, ItemRepository>();
```

## 6. Run the API

```shell
dotnet run
```

The generated endpoint is mapped to `GET /items/{id:guid}`.

## Customizing an endpoint

Each operation also generates an abstract `<OperationId>EndpointConfigurationBase` class. A concrete configuration can apply policies that remain application concerns:

```csharp
public sealed class GetItemEndpointConfiguration
    : GetItemEndpointConfigurationBase
{
    public override void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint.RequireAuthorization();
    }
}
```

MinimalOpenAPI applies the authored contract metadata first and invokes the application configuration last. The same hook can configure rate limiting, antiforgery, request-size limits, tags, and other ASP.NET Core endpoint metadata.

## Publishing the authored OpenAPI document

MinimalOpenAPI can expose the original contract without regenerating it from C#.

Add publishing metadata:

```xml
<OpenApi Include="openapi.yaml"
         PublishAs="/openapi/schema.yaml"
         DisplayName="Items API"
         DisplayVersion="1.0.0" />
```

Then map the document:

```csharp
var schemas = app.MapOpenApiSchemas();
```

The returned descriptors can be passed to Swagger UI, Scalar, or another OpenAPI viewer.

## Next steps

- Review the [supported feature matrix](schema-feature-roadmap.md).
- Explore the focused projects under [`sample/`](../sample/).
- Read the [architecture guide](architecture.md) for generated structure and diagnostics.
- Use the [consumer agent guide](consumer-agents.md) when delegating integration work to a coding agent.
