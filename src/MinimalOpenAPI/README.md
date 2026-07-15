# MinimalOpenAPI

MinimalOpenAPI is a **contract-first OpenAPI framework for ASP.NET Core Minimal APIs**.

You author an OpenAPI 3.0 or 3.1 document in YAML or JSON. At build time, MinimalOpenAPI generates the C# contracts, handler base classes, dependency-injection registration, and endpoint mapping required to implement the API.

## Requirements

- .NET 10
- ASP.NET Core

## Minimal setup

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.0.0" />
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

Implement the generated `<OperationId>EndpointBase` class for each operation. No manual route or handler registration is required.

## What the package includes

- Roslyn incremental source generator
- ASP.NET Core runtime services
- YAML and JSON OpenAPI parsers
- contract records and enums
- typed handler signatures and results
- abstract endpoint configuration base classes applied after contract metadata
- authored-schema publishing support

`MinimalOpenAPI` is the only package consumers need. Parser and abstraction assemblies are bundled as implementation details.

## Selected features

- OpenAPI 3.0 and 3.1
- YAML and JSON documents
- multiple documents per project
- component and inline object schemas
- inline objects used as array items
- enums, dictionaries, validation metadata, and `allOf` flattening
- `readOnly` and `writeOnly` request/response contract splitting
- path, query, header, reusable, and path-level parameters
- parameter default values
- typed results and `application/problem+json` wrappers
- `multipart/form-data`, `IFormFile`, multiple files, and nested form objects
- deterministic schema-name normalization with compile-time collision diagnostics
- explicit schema publication through `PublishAs` and `MapOpenApiSchemas()`

## Directional contracts

```xml
<OpenApi Include="openapi.yaml"
         ReadWriteSchemaHandling="Auto" />
```

- `Ignore` keeps neutral contracts.
- `Auto` creates request/response variants only when reachable `readOnly` or `writeOnly` properties require different shapes.
- `Split` always generates request and response graphs from operation body roots.

## Publishing authored schemas

```xml
<OpenApi Include="openapi.yaml"
         PublishAs="/openapi/schema.yaml"
         DisplayName="Todo API"
         DisplayVersion="1.0.0" />
```

```csharp
app.MapMinimalOpenApiEndpoints();
var schemas = app.MapOpenApiSchemas();
```

The returned descriptors can be used to configure Swagger UI, Scalar, or another OpenAPI viewer. MinimalOpenAPI serves the authored document; it does not generate a second document from C# at runtime.

## More documentation

Full documentation, samples, limitations, architecture notes, and release guidance are available in the repository:

https://github.com/Kralizek/MinimalOpenApi
