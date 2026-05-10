# Parameters

This sample concentrates all parameter-related behavior in one place.

## What it demonstrates

| Feature | Where |
|---------|-------|
| Component parameters (`$ref`) | `openapi.yaml` `components/parameters`, all operations |
| Path-level parameters | `openapi.yaml` `/tenants/{tenantId}/items` `parameters` block |
| Operation-level override of path-level parameter | `openapi.yaml` `createTenantItem` operation |
| Path route constraints from schema format | `tenantId` with `format: uuid` → `{tenantId:guid}` |
| Query parameters grouped into `Parameters` record | `page`, `pageSize`, `api-version` |
| Header parameters | `X-Correlation-Id`, `X-Contact-Email`, `X-Contact-Uri` |
| `format: email` / `format: uri` annotations | `ContactEmail`, `ContactUri` parameters |
| Parameter default values | `page: default: 1`, `pageSize: default: 20`, `api-version: default: "1.0"` |
| Cookie parameters | `sessionId` — see note below |

## Cookie parameter binding limitation

Cookie parameters (`in: cookie`) appear in the generated `Parameters` record, but **ASP.NET Core Minimal APIs do not automatically bind cookie parameters** the way they do path/query/header parameters. You must read cookie values manually from `HttpContext.Request.Cookies` in your handler.

The generator emits the cookie parameter in the record for visibility and documentation, but binding at runtime requires extra work.

## Interesting files

| File | What to look at |
|------|----------------|
| `openapi.yaml` | `components/parameters` and path-level `parameters` blocks |
| `ListTenantItemsHandler.cs` | Reading grouped `Parameters` record in a handler |
| `CreateTenantItemHandler.cs` | Operation-level parameter override |

## How to run

```shell
cd sample/Parameters
dotnet run
```

Then try:

```shell
# List items with query params
curl "http://localhost:5000/tenants/00000000-0000-0000-0000-000000000001/items?page=2&pageSize=10"

# With header params
curl "http://localhost:5000/tenants/00000000-0000-0000-0000-000000000001/items" \
  -H "X-Correlation-Id: my-correlation-id" \
  -H "X-Contact-Email: user@example.com"

# Create an item
curl -X POST "http://localhost:5000/tenants/00000000-0000-0000-0000-000000000001/items" \
  -H "Content-Type: application/json" \
  -d '{"name":"New Item"}'
```

## Generated types

- `MinimalOpenAPI.Samples.Parameters.Openapi.Endpoints.ListTenantItemsEndpointBase`
- `MinimalOpenAPI.Samples.Parameters.Openapi.Endpoints.ListTenantItemsEndpointBase.Parameters` — groups all non-path params
- `MinimalOpenAPI.Samples.Parameters.Openapi.Endpoints.CreateTenantItemEndpointBase`
- `MinimalOpenAPI.Samples.Parameters.Openapi.Contracts.OkResponse`
