# Installing and configuring a consumer application

Target .NET 10 with ASP.NET Core. Use the `MinimalOpenAPI` NuGet package only; parsers and abstractions are bundled internals. Select a current stable package version for new applications rather than copying a historical documentation version.

```xml
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="1.1.0" />
  <OpenApi Include="contracts/orders/openapi.yaml"
           Namespace="Orders"
           ReadWriteSchemaHandling="Auto"
           PublishAs="/openapi/orders.yaml"
           DisplayName="Orders API"
           DisplayVersion="1.0.0" />
</ItemGroup>
```

The version here illustrates the 1.1 feature set, not a policy to downgrade or freeze dependencies. Do not put parser or abstraction package references into consumer projects.

```csharp
using MinimalOpenAPI;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalOpenApi();
var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
var published = app.MapOpenApiSchemas(); // only needed when publishing contracts
app.Run();
```

For conventions shared by **all** generated endpoints, configure the `RouteGroupBuilder` returned by `MapMinimalOpenApiEndpoints()`; use the generated per-operation configuration base for narrower policies. See [endpoint configuration](endpoint-configuration.md). All configured OpenAPI documents share the generated route group.

`PublishAs` is opt-in; without it the authored file is copied to output but not served over HTTP. `MapOpenApiSchemas()` returns descriptors for viewer integrations and serves authored documents, not a generated code-first spec.

For multiple documents choose distinct `Namespace` metadata if derived names collide. When configuring a `MinimalOpenApiHandler` against multiple documents, use `OpenApiFile` equal to the complete configured `OpenApi Include` path, not merely its basename. With one document `OpenApiFile` may be omitted; if supplied it is still validated.

`ReadWriteSchemaHandling`: `Ignore` is neutral, `Auto` creates directional request/response shapes when needed (default), and `Split` always creates directional graphs from body roots.

Inspect [main documentation](https://github.com/Kralizek/MinimalOpenApi/blob/master/README.md) and [getting started](https://github.com/Kralizek/MinimalOpenApi/blob/master/docs/getting-started.md) before adopting optional conventions.
