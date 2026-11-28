# ClassLibrary sample

The OpenAPI contracts and handlers live in **class libraries**; a separate web host only calls
`AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()`.

| Project | SDK | Shows |
|---------|-----|-------|
| [`Catalog`](Catalog) | `Microsoft.NET.Sdk` + `Microsoft.AspNetCore.App` framework reference, `ImplicitUsings` disabled | A contract library whose `RootNamespace` (`Contracts`) collides with a generated namespace segment. |
| [`Billing`](Billing) | `Microsoft.NET.Sdk` + framework reference | A second contract library whose `RootNamespace` equals its document `Namespace`, with an endpoint configuration. |
| [`Inventory.Contracts`](Inventory.Contracts) | `Microsoft.NET.Sdk` + framework reference | `inventory.yaml` with `Generate="Contracts"`: only the DTOs. |
| [`Inventory.Api`](Inventory.Api) | `Microsoft.NET.Sdk` + framework reference | The same file with `Generate="Endpoints"`: handler and configuration bases, mapping and registration, built on the DTOs of `Inventory.Contracts`. It implements no handler. |
| [`Host`](Host) | `Microsoft.NET.Sdk.Web` | The executable. It declares its own small document, references the libraries, and implements the inventory handler and endpoint configuration ([`Host/Inventory`](Host/Inventory)). |

The host does not reference any library type in `Program.cs`. The generator running in the host
discovers each referenced library's generated entry point (`CatalogMinimalOpenApi`,
`BillingMinimalOpenApi`, `StockMinimalOpenApi`) at build time and registers it before `Main` runs.
It also registers the host's `GetStockHandler` and `GetStockEndpointConfiguration`, which derive from
bases generated in `Inventory.Api`, in place of that library's default handler.

```shell
dotnet run --project sample/ClassLibrary/Host
curl http://localhost:5000/catalog/items/42
curl http://localhost:5000/billing/invoices/7
curl http://localhost:5000/inventory/stock/abc
curl http://localhost:5000/health
curl http://localhost:5000/openapi/billing.yaml   # authored documents, served from the host's output
```

End-to-end coverage: `tests/MinimalOpenAPI.ClassLibrary.IntegrationTests` (project references) and
`sample/ClassLibrarySmokeTest` (packed NuGet package).
