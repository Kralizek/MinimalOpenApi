# MinimalOpenAPI

**Start here.** Contract-first OpenAPI framework for ASP.NET Core Minimal APIs.

Adding this single package to your project gives you:

| What you get | From |
|---|---|
| Roslyn source generator (build-time code generation) | Included in this package |
| `AddMinimalOpenApi()` / `MapEndpoints()` | `MinimalOpenAPI.Runtime` (declared dependency) |

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
