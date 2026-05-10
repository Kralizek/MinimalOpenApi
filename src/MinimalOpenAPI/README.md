# MinimalOpenAPI

MinimalOpenAPI is a **contract-first OpenAPI framework for ASP.NET Core Minimal APIs**.

You author the OpenAPI document first. MinimalOpenAPI reads it at build time and generates the C# scaffolding needed to implement the API.

## What the package includes

| What you get | Notes |
|---|---|
| Roslyn source generator | Generates DTOs, handler base classes, endpoint mapping, and DI registration at build time. |
| Runtime services | Provides `AddMinimalOpenApi()`, `MapMinimalOpenApiEndpoints()`, and `MapOpenApiSchemas()`. |
| YAML and JSON parser support | Use `<OpenApi Include="openapi.yaml" />` or `<OpenApi Include="openapi.json" />`. |
| Spec publishing support | Copies authored OpenAPI files to build/publish output and can serve selected specs over HTTP. |

## Minimal setup

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0-beta.1" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

```csharp
using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
app.Run();
```

Implement the generated abstract handler base class for each operation in your OpenAPI spec. The generator takes care of route mapping, handler registration, DTO generation, and endpoint metadata.

## Generated contracts

MinimalOpenAPI generates DTO records from `components/schemas`.

By default, OpenAPI `readOnly` and `writeOnly` properties are respected through `ReadWriteSchemaHandling="Auto"`:

```xml
<OpenApi Include="openapi.yaml"
         ReadWriteSchemaHandling="Auto" />
```

Supported values:

- `Ignore`: parse `readOnly` / `writeOnly`, but keep neutral DTO shapes.
- `Auto`: default. Generate request/response DTO variants only when direct or reachable `readOnly` / `writeOnly` properties require different shapes.
- `Split`: generate request/response DTO graphs from operation body roots even when the current shapes are identical, preserving contract type names if directional properties are added later.

For example, with `Account.id: readOnly` and `Account.password: writeOnly`, generated operation signatures use `AccountRequest` and `AccountResponse`.

## Publishing and serving specs

Every `<OpenApi />` item is copied to build and publish output under an internal collision-safe path:

```text
openapi/schemas/<SchemaId>/<filename>
```

To expose a spec over HTTP, configure `PublishAs`:

```xml
<OpenApi Include="openapi.yaml"
         PublishAs="/openapi/schema.yaml"
         DisplayName="Todo API"
         DisplayVersion="1.0.0" />
```

Then map schema endpoints:

```csharp
app.MapMinimalOpenApiEndpoints();
var schemas = app.MapOpenApiSchemas();
```

Rules:

- `PublishAs` must start with `/`.
- `PublishAs` values must be unique across all `<OpenApi />` items.
- OpenAPI files without `PublishAs` are copied to output/publish, but are not served over HTTP.
- `DisplayName` and `DisplayVersion` are optional descriptor metadata.

`MapOpenApiSchemas()` returns descriptors with `PublicPath`, `Name`, `Version`, `FullName`, and `Endpoint`, which can be used to configure Swagger UI, Scalar, or another OpenAPI UI package.

## More documentation

For full documentation, samples, architecture notes, and release guidance, visit the repository:

https://github.com/Kralizek/MinimalOpenApi
