# Consumer Agent Guide — MinimalOpenAPI

This guide is for coding agents integrating `MinimalOpenAPI` into an ASP.NET Core application.

## Purpose

MinimalOpenAPI is a **contract-first** framework for ASP.NET Core Minimal APIs.

- The OpenAPI document is the source of truth.
- A Roslyn incremental generator reads the document at build time.
- The generator emits contracts, handler base classes, DI registration, endpoint mapping, and endpoint metadata.
- Consumer code implements the generated handler base classes.

Do not treat the generated C# as the primary contract and do not edit generated files.

## Package selection

Reference only the `MinimalOpenAPI` package:

```xml
<PackageReference Include="MinimalOpenAPI" Version="1.0.0" />
```

The package contains both the source generator and the required ASP.NET Core runtime services. `MinimalOpenAPI.Abstractions`, `MinimalOpenAPI.Parser.Yaml`, and `MinimalOpenAPI.Parser.Json` are bundled implementation assemblies, not separately published consumer packages.

## Requirements

- .NET 10
- ASP.NET Core
- OpenAPI 3.0 or 3.1 in YAML or JSON

## Minimal integration

### 1. Add the package and document

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

### 2. Build before writing handlers

```shell
dotnet build
```

The generated types become available to the compilation after the first successful build.

### 3. Register services and endpoints

```csharp
using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
app.Run();
```

Call `AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()` once per application.

### 4. Implement each generated handler

Given:

```yaml
paths:
  /todos/{id}:
    get:
      operationId: getTodo
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: Found
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/Todo"
        "404":
          description: Not found
```

Implement the generated base class:

```csharp
public sealed class GetTodoEndpoint(ITodoStore store)
    : GetTodoEndpointBase
{
    public override async Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var todo = await store.FindAsync(id, cancellationToken);
        return todo is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(todo);
    }
}
```

Do not call `base.HandleAsync(...)`; the base implementation throws `NotImplementedException` by design.

## Generated namespaces

Each OpenAPI document gets a namespace segment derived from the file name:

```text
{RootNamespace}.{SpecName}.Contracts
{RootNamespace}.{SpecName}.Endpoints
```

When multiple files would derive the same spec name, set explicit `Namespace` metadata:

```xml
<ItemGroup>
  <OpenApi Include="apis/orders/openapi.yaml" Namespace="Orders" />
  <OpenApi Include="apis/payments/openapi.yaml" Namespace="Payments" />
</ItemGroup>
```

Duplicate resolved spec names produce `MOA009`.

## Generated shapes

Agents should inspect the generated signatures rather than guessing them.

General rules:

- component object schemas produce records;
- string enums produce C# enums;
- inline request bodies produce nested `Request` records;
- non-path parameters are grouped into a nested `Parameters` record;
- path parameters are direct handler arguments;
- multiple response statuses produce `Results<T1, T2, ...>`;
- inline response objects produce status-specific nested records;
- `application/problem+json` produces status-specific typed wrappers;
- `additionalProperties` maps to `Dictionary<string, T>`;
- inline object array items produce generated item records rather than `object[]`;
- `allOf` object branches are flattened into one contract shape.

When exact generated source is needed, enable Roslyn generated-file emission as documented in the repository README.

## `readOnly` and `writeOnly`

The default mode is `Auto`:

```xml
<OpenApi Include="openapi.yaml"
         ReadWriteSchemaHandling="Auto" />
```

- `Ignore` keeps neutral contracts.
- `Auto` generates request/response variants only when reachable directional properties require different shapes.
- `Split` always generates request and response graphs from operation body roots.

Do not assume a component type such as `Account` is always used directly in an operation. Build and use the generated `AccountRequest` or `AccountResponse` type when the handler signature requires it.

## Schema-name normalization

OpenAPI component keys are normalized deterministically into valid C# type names.

Examples:

```text
Billing.InvoiceStatus -> BillingInvoiceStatus
123-invoice           -> Value123Invoice
order_item             -> OrderItem
```

Always keep `$ref` values pointed at the original OpenAPI key. The generator resolves the original key and applies the shared name map internally.

Do not recreate the normalization algorithm in consumer code. Name collisions are reported through `MOA012`–`MOA014` and must be fixed in the contract.

## Multipart request bodies

A `multipart/form-data` request generates a form-bound nested `Request` record.

- `type: string`, `format: binary` maps to `IFormFile`;
- an array of binary strings maps to `IReadOnlyList<IFormFile>`;
- scalar fields use normal primitive mappings;
- nested objects bind through dotted keys such as `metadata.title`.

Use the generated request type exactly as emitted. Do not replace it with a manually authored form model.

Array-of-object and dictionary form fields produce `MOA011`. Restructure the contract rather than bypassing the diagnostic unless the endpoint is intentionally implemented outside MinimalOpenAPI.

Antiforgery, request-size limits, file validation, and storage remain application concerns. Configure them through ASP.NET Core and a concrete class inheriting from the generated `<OperationId>EndpointConfigurationBase` type.

## Endpoint configuration

For operation-specific policies, inherit from the generated abstract `<OperationId>EndpointConfigurationBase` type and override `Configure(RouteHandlerBuilder endpoint)`.

MinimalOpenAPI applies contract-derived endpoint metadata before invoking this method, so the application configuration is the final configuration layer. Configure authorization, rate limiting, request limits, antiforgery, OpenAPI metadata, and other endpoint conventions here rather than editing generated mapping code.

Only one concrete configuration may exist per operation; duplicates produce `MOA003`.

## Publishing the authored document

Configure an explicit public path:

```xml
<OpenApi Include="openapi.yaml"
         PublishAs="/openapi/schema.yaml"
         DisplayName="Todo API"
         DisplayVersion="1.0.0" />
```

Map published documents:

```csharp
app.MapMinimalOpenApiEndpoints();
var schemas = app.MapOpenApiSchemas();
```

Every document is copied to build and publish output. Only documents with `PublishAs` are exposed over HTTP.

`MapOpenApiSchemas()` returns descriptors containing `PublicPath`, `Name`, `Version`, `FullName`, and `Endpoint`. These can be passed to Swagger UI, Scalar, or another viewer.

Do not add runtime OpenAPI generation unless the application intentionally needs a separate code-first document.

## Key invariants

Agents must preserve these rules:

1. Change the OpenAPI document before changing generated contract behavior.
2. Rebuild after every contract change.
3. Never edit files under `obj/` or files marked `// <auto-generated/>`.
4. Implement generated handler base classes instead of duplicating generated routes.
5. Keep exactly one concrete handler per generated operation.
6. Keep exactly one concrete endpoint configuration per generated operation.
7. Do not add manual `app.Map*` routes for operations already generated.
8. Do not reference bundled parser or abstraction assemblies directly from consumer projects.
9. Use generated request, response, parameter, and result types exactly as required by the handler signature.
10. Fix generator diagnostics in the contract or implementation rather than suppressing them without analysis.

## Supported surface to consider before designing workarounds

MinimalOpenAPI 1.0 supports:

- OpenAPI 3.0 and 3.1;
- YAML and JSON;
- multiple documents;
- nested inline objects and inline array item objects;
- enums, dictionaries, validation metadata, and `allOf`;
- `readOnly` and `writeOnly` splitting;
- reusable and path-level parameters;
- parameter defaults;
- typed problem responses;
- multipart files and nested form objects;
- schema publishing and viewer integration;
- deterministic component-name normalization.

Before adding manual models or endpoints, check the repository README, samples, and feature support matrix.

## Current limitations

- `oneOf` and `anyOf` are not supported.
- Runtime `DataAnnotations` validation is not automatically executed by ASP.NET Core Minimal APIs.
- Cookie parameters require application-specific binding or `HttpContext` access.
- Multipart arrays of objects and dictionaries are not supported.
- Non-JSON text and binary response families are not yet generated.
- OpenAPI 2.0 is not supported.

## Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| Generated types are missing | Missing or invalid `<OpenApi>` item | Verify the path and rebuild. |
| `MOA001` | No concrete handler implementation | Inherit from the generated endpoint base and override `HandleAsync`. |
| `MOA002` | Duplicate handlers | Keep one concrete handler for the operation. |
| `MOA003` | Duplicate endpoint configuration implementations | Keep one concrete configuration. |
| `MOA004` | YAML/JSON parse failure | Validate and minimize the document. |
| `MOA008` | Unresolved parameter `$ref` | Correct the reference to `components/parameters`. |
| `MOA009` | Duplicate resolved document namespace | Add unique `Namespace` metadata. |
| `MOA010` | Invalid read/write mode | Use `Ignore`, `Auto`, or `Split`. |
| `MOA011` | Unsupported multipart field shape | Restructure arrays of objects or dictionaries. |
| `MOA012`–`MOA014` | Generated-name collision | Rename the conflicting schema or property in OpenAPI. |
| `NotImplementedException` at runtime | Missing override or call to base implementation | Override `HandleAsync` and remove the base call. |
| Duplicate route at startup | Manual and generated mapping both register the operation | Remove the manual mapping. |

## Existing projects

When integrating into an established application:

1. Inspect existing route registration, DI, authorization, validation, and error-handling conventions.
2. Add new contract-first endpoints without rewriting unrelated endpoints.
3. Avoid registering the same route through multiple frameworks.
4. Place handler and endpoint configuration classes according to the project's existing structure.
5. Add focused build or integration tests for each generated operation.
6. Preserve the authored OpenAPI document in source control and review it as an API contract.
