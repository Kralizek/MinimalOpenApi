# MinimalOpenAPI

MinimalOpenAPI is a **contract-first** OpenAPI framework for ASP.NET Core Minimal APIs.

Define your API surface in an OpenAPI (YAML or JSON) file. The Roslyn source generator reads it and automatically produces strongly-typed handler base classes, DTO records, DI registration, and endpoint mapping code. You only need to inherit the generated base class and fill in the business logic.

For a deep-dive into the design, architecture, and internals see
[docs/architecture.md](docs/architecture.md).

## How it works

```
openapi.yaml  ──►  [MinimalOpenAPI]  ──►  Generated C#
openapi.json  ──►       (build time)          │
                                              ├─ DTO records
                                              ├─ Abstract handler base classes
                                              ├─ DI registration
                                              └─ Endpoint mapping
```

## Quick start

**1 — Add the package and your OpenAPI spec:**

```xml
<!-- MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="*" />
  <OpenApi Include="openapi.yaml" />  <!-- or openapi.json -->
</ItemGroup>
```

**2 — Register the services and map the endpoints:**

```csharp
// Program.cs
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapMinimalOpenApiEndpoints();
```

**3 — Implement the generated handler base class:**

```csharp
// GetItemEndpoint.cs
public class GetItemEndpoint(IItemStore store) : GetItemEndpointBase   // generated base class
{
    public override async Task<Results<Ok<Item>, NotFound>> HandleAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var item = await store.FindAsync(id, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }
}
```

That's it — no manual route registration, no manual DI wiring.

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [`MinimalOpenAPI`](src/MinimalOpenAPI) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI)](https://www.nuget.org/packages/MinimalOpenAPI) | **Start here.** Includes the Roslyn source generator and declares `MinimalOpenAPI.Runtime` as a dependency. |
| [`MinimalOpenAPI.Runtime`](src/MinimalOpenAPI.Runtime) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Runtime)](https://www.nuget.org/packages/MinimalOpenAPI.Runtime) | ASP.NET Core runtime services (`AddMinimalOpenApi`, `MapMinimalOpenApiEndpoints`). |
| [`MinimalOpenAPI.Abstractions`](src/MinimalOpenAPI.Abstractions) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Abstractions)](https://www.nuget.org/packages/MinimalOpenAPI.Abstractions) | OpenAPI document model and parser abstractions. |
| [`MinimalOpenAPI.Parser.Yaml`](src/MinimalOpenAPI.Parser.Yaml) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Parser.Yaml)](https://www.nuget.org/packages/MinimalOpenAPI.Parser.Yaml) | YAML parser for OpenAPI specs, built on YamlDotNet. |
| [`MinimalOpenAPI.Parser.Json`](src/MinimalOpenAPI.Parser.Json) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Parser.Json)](https://www.nuget.org/packages/MinimalOpenAPI.Parser.Json) | JSON parser for OpenAPI specs, built on System.Text.Json. |

### Nightly builds

Pre-release packages built from every commit to `master` are published to the GitHub Packages NuGet feed:

```
https://nuget.pkg.github.com/Kralizek/index.json
```

## Repository structure

```
src/
  MinimalOpenAPI/               ← MinimalOpenAPI NuGet package (generator + runtime dep)
  MinimalOpenAPI.Runtime/       ← runtime services
  MinimalOpenAPI.Abstractions/  ← document model & parser contracts
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser implementation
  MinimalOpenAPI.Parser.Json/   ← JSON parser implementation
sample/
  MinimalOpenAPI.Sample.Api/    ← end-to-end example
tests/
  MinimalOpenAPI.Generator.Tests/
  MinimalOpenAPI.Runtime.Tests/
  MinimalOpenAPI.IntegrationTests/
```

## Contributing

Contributions are welcome. Please open an issue first to discuss proposed changes.

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-change`).
3. Commit your changes.
4. Open a pull request against `master`.

The CI pipeline enforces a warning-free build (`--warnaserror`) and runs all unit and integration tests.
