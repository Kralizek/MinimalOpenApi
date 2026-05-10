# SmokeTest

This project is a **CI/package-consumption sample**, not the recommended starting point for learning MinimalOpenAPI.

## Purpose

- Validate consuming the packed `MinimalOpenAPI` NuGet artifact via `PackageReference`.
- Validate `buildTransitive` packaging (source generator + targets are transitively imported).
- Validate the source generator and runtime services in a real downstream project.
- Validate that publish output includes copied OpenAPI schemas.

## What it covers

| Feature | Where |
|---------|-------|
| Basic endpoint generation | `PingEndpoint.cs` |
| Inline complex `additionalProperties` | `SetLabelsEndpoint.cs`, `openapi.yaml` `/labels` |
| `readOnly` / `writeOnly` scoped DTOs | `CreateAccountEndpoint.cs`, `openapi.yaml` `Account` |
| Reusable component parameters | `GetTenantItemsEndpoint.cs`, `openapi.yaml` `components/parameters` |
| Explicit `PublishAs` metadata | `SmokeTest.csproj` |

## How to run

This project requires the packed NuGet artifact to be in the `artifacts/` directory at the repository root. Run from the repo root:

```shell
dotnet build --configuration Release
dotnet pack src/MinimalOpenAPI/MinimalOpenAPI.csproj --no-build --configuration Release --output artifacts
dotnet restore sample/SmokeTest/SmokeTest.csproj
dotnet build sample/SmokeTest/SmokeTest.csproj --no-restore --configuration Release --warnaserror
dotnet publish sample/SmokeTest/SmokeTest.csproj --no-restore --configuration Release --output smoketest-publish
```

Verify the published schema:

```shell
find smoketest-publish/openapi/schemas -name "openapi.yaml"
```

## Note for learning

If you want to learn MinimalOpenAPI, start with [BasicTodo](../BasicTodo/README.md) instead.

---

← [Back to sample catalog](../README.md)
