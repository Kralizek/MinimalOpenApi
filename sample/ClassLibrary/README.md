# ClassLibrary sample

The OpenAPI contracts and handlers live in **class libraries**; a separate web host only calls
`AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()`.

| Project | SDK | Shows |
|---------|-----|-------|
| [`Catalog`](Catalog) | `Microsoft.NET.Sdk` + `Microsoft.AspNetCore.App` framework reference, `ImplicitUsings` disabled | A contract library whose `RootNamespace` (`Contracts`) collides with a generated namespace segment. |
| [`Billing`](Billing) | `Microsoft.NET.Sdk` + framework reference | A second contract library whose `RootNamespace` equals its document `Namespace`, with an endpoint configuration. |
| [`Host`](Host) | `Microsoft.NET.Sdk.Web` | The executable. It declares its own small document and references both libraries. |

The host does not reference any library type in `Program.cs`. The generator running in the host
discovers each referenced library's generated entry point (`CatalogMinimalOpenApi`,
`BillingMinimalOpenApi`) at build time and registers it before `Main` runs.

```shell
dotnet run --project sample/ClassLibrary/Host
curl http://localhost:5000/catalog/items/42
curl http://localhost:5000/billing/invoices/7
curl http://localhost:5000/health
```

End-to-end coverage: `tests/MinimalOpenAPI.ClassLibrary.IntegrationTests` (project references) and
`sample/ClassLibrarySmokeTest` (packed NuGet package).
