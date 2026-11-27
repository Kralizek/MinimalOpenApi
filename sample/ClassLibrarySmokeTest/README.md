# ClassLibrarySmokeTest

A **CI/package-consumption sample** for the class-library layout: the OpenAPI contracts and
handlers live in class libraries that consume the packed `MinimalOpenAPI` NuGet artifact, and a
separate web host references only the libraries.

| Project | Shows |
|---------|-------|
| `Catalog` | `Microsoft.NET.Sdk` library with `ImplicitUsings` disabled and `RootNamespace=Contracts`, which collides with a generated namespace segment. |
| `Billing` | Second library whose `RootNamespace` equals its document `Namespace`, with an endpoint configuration. |
| `Host` | `Microsoft.NET.Sdk.Web` host with **no direct** `MinimalOpenAPI` package reference: the package, its source generator and its `buildTransitive` targets flow from the libraries. It declares its own small document and calls only `AddMinimalOpenApi()` / `MapMinimalOpenApiEndpoints()`. |

The sources and OpenAPI documents are shared with [`sample/ClassLibrary`](../ClassLibrary), which
uses project references instead of the package.

`scripts/smoke-test-class-library.sh` restores, builds (`--warnaserror`) and publishes the host
against `artifacts/`, then **runs the published host** and checks every document's endpoints over
HTTP.
