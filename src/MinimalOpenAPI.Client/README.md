# MinimalOpenAPIClient

`MinimalOpenAPIClient` generates strongly typed .NET HTTP clients directly from OpenAPI 3.x JSON or YAML contracts.

Consumers require .NET 10 and C# 11 or newer. ASP.NET Core is not required. The package supplies the `Microsoft.Extensions.Http` dependency used by the generated registration helpers.

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
    public Task<Todo> GetAsync(Guid id, CancellationToken cancellationToken)
        => client.GetTodoAsync(id, cancellationToken);
}
```

The client package is intentionally named `MinimalOpenAPIClient`, rather than `MinimalOpenAPI.Client`: it is the client-side counterpart to MinimalOpenAPI, not a runtime submodule of the server package.

## Type generation

Client types represent the OpenAPI wire contract and intentionally do not attempt to reuse server CLR types. Component schemas retain their schema names. Inline request and response schemas are named from the operation (`CreateTodoRequest`, `CreateTodoResponse`), and nested inline schemas are qualified by their owning generated type. Name collisions are resolved deterministically.

Names are normalized to valid C# identifiers; reserved names receive deterministic suffixes while JSON property and enum wire names are preserved. Reachable `readOnly` and `writeOnly` properties produce request/response variants where needed, such as `AccountRequest` and `AccountResponse`. Required properties use C# `required`, including nullable and value-type properties.

Required method arguments precede optional arguments. Optional parameters and request bodies default to `null`; omitted parameters are not sent. Optional DTO properties set to `null` are omitted from JSON; required nullable properties retain explicit `null` values. Base-address paths are preserved with or without a trailing slash, without modifying the supplied `HttpClient`.

## Supported contract subset

- JSON request bodies using `application/json`; JSON success bodies using `application/json` or `application/problem+json`; bodyless success responses. An omitted `content` member and a declared empty map (`content: {}`) are both treated as bodyless, while the parser still preserves whether the map was declared.
- Primitive, nullable, component and inline object bodies, arrays, string enums, dictionary schemas, and compatible `allOf` object composition.
- Scalar path, query, header and cookie parameters, plus exploded form-style query arrays (including component references). Date and timestamp parameters use ISO wire formats.
- Multiple success statuses when every success response has the same generated body type. Incompatible success shapes, including mixed JSON and bodyless responses, produce `MOAC003`.
- Non-success HTTP statuses throw the generated `<SpecName>ClientException`, retaining the status and response text. Empty or invalid JSON bodies throw `JsonException`; JSON `null` is accepted only for nullable response schemas.

Multipart and other non-JSON bodies, object parameters, array path/header/cookie parameters, non-default parameter styles, `explode: false` query arrays, and `allowReserved: true` are diagnosed as unsupported. External references, unresolved references, recursive collection aliases, and unsupported success response definitions also produce diagnostics. Use inline numeric success-status response definitions; response references, wildcard/default response modeling, `oneOf`, and `anyOf` are not supported. Authentication, retries, and other transport policies remain application-owned through `HttpClient` and dependency injection.

Diagnostics: `MOAC001` for parse failures, `MOAC002` for unsupported file extensions, `MOAC003` for unsupported contracts or invalid namespace metadata, and `MOAC004` for duplicate generated namespaces. Each document must have a distinct namespace.

## Why the client is generated directly

The client generator emits concrete `HttpClient` code rather than Refit interfaces. Refit would require the output of one source generator to become input to another source generator, which Roslyn does not support as a reliable pipeline in the same compilation. Avoiding that intermediate representation also keeps the generated transport aligned directly with the OpenAPI contract and avoids a Refit runtime/generator dependency.
