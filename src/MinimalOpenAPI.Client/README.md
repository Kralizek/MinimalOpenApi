# MinimalOpenAPI.Client

`MinimalOpenAPI.Client` generates strongly typed .NET HTTP clients directly from OpenAPI 3.x JSON or YAML contracts.

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI.Client" Version="..." />

  <OpenApiClient Include="openapi.yaml"
                 Namespace="MyApp.Backend" />
</ItemGroup>
```

A normal build generates client-side DTOs, a concrete `HttpClient`-based client, a typed exception for non-success responses, and `IServiceCollection` registration helpers. Generated files are compiler outputs and do not need to be checked into source control.

For `openapi.yaml`, the generated client is named `OpenapiClient` by default. Operation method names come from `operationId` and are suffixed with `Async`.

```csharp
services.AddOpenapiClient(new Uri("https://api.example.com"));

public sealed class MyService(OpenapiClient client)
{
    public Task<GetTodoResponse> GetAsync(Guid id, CancellationToken cancellationToken)
        => client.GetTodoAsync(id, cancellationToken);
}
```

## Type generation

Client types represent the OpenAPI wire contract and intentionally do not attempt to reuse server CLR types. Component schemas retain their schema names. Inline request and response schemas are named from the operation (`CreateTodoRequest`, `CreateTodoResponse`), and nested inline schemas are qualified by their owning generated type. Name collisions are resolved deterministically.

## Why the client is generated directly

The client generator emits concrete `HttpClient` code rather than Refit interfaces. Refit would require the output of one source generator to become input to another source generator, which Roslyn does not support as a reliable pipeline in the same compilation. Avoiding that intermediate representation also keeps the generated transport aligned directly with the OpenAPI contract and avoids a Refit runtime/generator dependency.
