# MinimalOpenAPIClient

`MinimalOpenAPIClient` generates strongly typed .NET HTTP clients directly from OpenAPI 3.x JSON or YAML contracts.

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPIClient" Version="..." />

  <OpenApiClient Include="openapi.yaml" />
</ItemGroup>
```

A normal build generates client-side DTOs, a concrete `HttpClient`-based client, a typed exception for non-success responses, and `IServiceCollection` registration helpers. Generated files are compiler outputs and do not need to be checked into source control.

For `openapi.yaml`, the generated client is named `OpenapiClient` by default. Operation method names come from `operationId` and are suffixed with `Async`.

By default, generated client code is placed under `{RootNamespace}.Clients.{SpecName}`. This keeps outbound API clients distinct from MinimalOpenAPI server-side contracts and endpoints when the same service both exposes an API and consumes another one.

For example, with a project root namespace of `MyApp` and `backend.yaml`, generated code lives in:

```text
MyApp.Clients.Backend
```

You can override the generated namespace explicitly when needed:

```xml
<OpenApiClient Include="backend.yaml"
               Namespace="MyApp.External.Backend" />
```

```csharp
using MyApp.Clients.Backend;

services.AddBackendClient(new Uri("https://api.example.com"));

public sealed class MyService(BackendClient client)
{
    public Task<GetTodoResponse> GetAsync(Guid id, CancellationToken cancellationToken)
        => client.GetTodoAsync(id, cancellationToken);
}
```

The client package is intentionally named `MinimalOpenAPIClient`, rather than `MinimalOpenAPI.Client`: it is the client-side counterpart to MinimalOpenAPI, not a runtime submodule of the server package.

## Type generation

Client types represent the OpenAPI wire contract and intentionally do not attempt to reuse server CLR types. Component schemas retain their schema names. Inline request and response schemas are named from the operation (`CreateTodoRequest`, `CreateTodoResponse`), and nested inline schemas are qualified by their owning generated type. Name collisions are resolved deterministically.

## Why the client is generated directly

The client generator emits concrete `HttpClient` code rather than Refit interfaces. Refit would require the output of one source generator to become input to another source generator, which Roslyn does not support as a reliable pipeline in the same compilation. Avoiding that intermediate representation also keeps the generated transport aligned directly with the OpenAPI contract and avoids a Refit runtime/generator dependency.