# MinimalOpenAPI

**Start here.** Meta-package for the [MinimalOpenAPI](https://github.com/Kralizek/MinimalOpenApi) contract-first framework.

Adding this single package to your ASP.NET Core project gives you everything you need:

| What you get | Package |
|---|---|
| `AddMinimalOpenApi()` / `MapEndpoints()` | `MinimalOpenAPI.Runtime` |
| Roslyn source generator (build-time code generation) | `MinimalOpenAPI.Generator` |

## Usage

```xml
<!-- MyApp.csproj -->
<ItemGroup>
  <PackageReference Include="MinimalOpenAPI" Version="*" />
  <OpenApi Include="openapi.yaml" />
</ItemGroup>
```

```csharp
// Program.cs
builder.Services.AddMinimalOpenApi();
app.MapEndpoints();
```

Implement the generated abstract handler base class for each operation in your OpenAPI spec.
The source generator takes care of the rest.

For full documentation and examples, visit the [MinimalOpenAPI repository](https://github.com/Kralizek/MinimalOpenApi).
