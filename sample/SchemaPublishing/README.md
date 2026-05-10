# SchemaPublishing

This sample demonstrates the explicit `PublishAs` schema publishing model and Swagger UI wiring.

## What it demonstrates

- All `<OpenApi />` items are copied to build/publish output under `openapi/schemas/<SchemaId>/`
- Only specs with `PublishAs` are exposed as HTTP endpoints via `MapOpenApiSchemas()`
- `DisplayName` and `DisplayVersion` feed descriptor metadata (name, version) used in Swagger UI
- `MapOpenApiSchemas()` returns schema descriptors that you use to configure Swagger UI
- Swagger UI wiring using the returned schema descriptors

## Key concepts

| Concept | Where |
|---------|-------|
| `PublishAs` on `public-api.yaml` | `SchemaPublishing.csproj` — serves the schema over HTTP |
| No `PublishAs` on `internal-api.yaml` | `SchemaPublishing.csproj` — still copied to output, not served |
| `MapOpenApiSchemas()` call | `Program.cs` — maps the public schema and returns descriptors |
| Swagger UI setup | `Program.cs` — uses descriptors from `MapOpenApiSchemas()` |

## Interesting files

| File | What to look at |
|------|----------------|
| `SchemaPublishing.csproj` | `<OpenApi>` items with and without `PublishAs` |
| `Program.cs` | `MapOpenApiSchemas()` + Swagger UI wiring |
| `public-api.yaml` | The publicly served OpenAPI schema |
| `internal-api.yaml` | Copied to output but not served over HTTP |

## How to run

```shell
cd sample/SchemaPublishing
dotnet run
```

Then visit:

- **Swagger UI**: http://localhost:5000/swagger/index.html (shows only `public-api.yaml`)
- **Public schema**: http://localhost:5000/openapi/public-api.yaml
- **Internal schema** (build output only — not served): check `bin/Debug/net10.0/openapi/schemas/`

## Generated types

- `MinimalOpenAPI.Samples.SchemaPublishing.PublicApi.Endpoints.GetStatusEndpointBase`
- `MinimalOpenAPI.Samples.SchemaPublishing.InternalApi.Endpoints.GetInternalHealthEndpointBase`
