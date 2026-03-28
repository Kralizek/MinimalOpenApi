# MinimalOpenAPI

MinimalOpenAPI is a **contract-first** OpenAPI framework for ASP.NET Core Minimal APIs.

Define your API surface in an OpenAPI (YAML) file. The Roslyn source generator reads it and automatically produces strongly-typed handler base classes, DTO records, DI registration, and endpoint mapping code. You only need to inherit the generated base class and fill in the business logic.

## How it works

```
openapi.yaml  ──►  [MinimalOpenAPI.Generator]  ──►  Generated C#
                         (build time)                  │
                                                       ├─ DTO records
                                                       ├─ Abstract handler base classes
                                                       ├─ DI registration
                                                       └─ Endpoint mapping
```

## Quick start

**1 — Add the meta-package and your OpenAPI spec:**

```xml
<!-- MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="*" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

**2 — Register the services and map the endpoints:**

```csharp
// Program.cs
builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.MapEndpoints();
```

**3 — Implement the generated handler base class:**

```csharp
// GetItemHandler.cs
public class GetItemHandler : GetItemEndpoint   // generated base class
{
    public override async Task<Results<Ok<Item>, NotFound>> Handle(
        Guid id, CancellationToken cancellationToken)
    {
        var item = await _store.FindAsync(id, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }
}
```

That's it — no manual route registration, no manual DI wiring.

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| [`MinimalOpenAPI`](src/MinimalOpenAPI) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI)](https://www.nuget.org/packages/MinimalOpenAPI) | **Start here.** Meta-package that brings in the runtime and the source generator. |
| [`MinimalOpenAPI.Runtime`](src/MinimalOpenAPI.Runtime) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Runtime)](https://www.nuget.org/packages/MinimalOpenAPI.Runtime) | ASP.NET Core runtime services (`AddMinimalOpenApi`, `MapEndpoints`). |
| [`MinimalOpenAPI.Generator`](src/MinimalOpenAPI.Generator) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Generator)](https://www.nuget.org/packages/MinimalOpenAPI.Generator) | Roslyn incremental source generator. |
| [`MinimalOpenAPI.Abstractions`](src/MinimalOpenAPI.Abstractions) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Abstractions)](https://www.nuget.org/packages/MinimalOpenAPI.Abstractions) | OpenAPI document model and parser abstractions. |
| [`MinimalOpenAPI.Parser.Yaml`](src/MinimalOpenAPI.Parser.Yaml) | [![NuGet](https://img.shields.io/nuget/v/MinimalOpenAPI.Parser.Yaml)](https://www.nuget.org/packages/MinimalOpenAPI.Parser.Yaml) | YAML parser for OpenAPI specs, built on YamlDotNet. |

### Nightly builds

Pre-release packages built from every commit to `main` are published to the GitHub Packages NuGet feed:

```
https://nuget.pkg.github.com/Kralizek/index.json
```

## Repository structure

```
src/
  MinimalOpenAPI/               ← meta-package
  MinimalOpenAPI.Runtime/       ← runtime services
  MinimalOpenAPI.Generator/     ← Roslyn source generator
  MinimalOpenAPI.Abstractions/  ← document model & parser contracts
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser implementation
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
4. Open a pull request against `main`.

The CI pipeline enforces a warning-free build (`--warnaserror`) and runs all unit and integration tests.
