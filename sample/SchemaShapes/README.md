# SchemaShapes

This sample demonstrates how OpenAPI schema shapes become generated C# DTOs.

## What it demonstrates

| Feature | Where in `openapi.yaml` |
|---------|------------------------|
| Component schemas | `components/schemas` section |
| Enum generation | `AccountRole`, `ProductStatus` |
| Validation attributes | `name: minLength: 1, maxLength: 200`, etc. |
| `format: date` → `DateOnly` | `Product.availableFrom` |
| `format: email` / `format: uri` metadata | `Account.email`, `Account.profileUrl` |
| Nested inline object schemas | `Product.dimensions` |
| `additionalProperties` with primitive values | `Product.tags` → `Dictionary<string, string>` |
| `additionalProperties` with inline object values | `Product.attributes` → `Dictionary<string, ProductAttributesValue>` |
| `allOf` flattening | `CatalogDetail` = `Catalog` + inline extension |
| `readOnly` / `writeOnly` scoped DTOs | `Account.id` (readOnly), `Account.password` (writeOnly) |
| `ReadWriteSchemaHandling="Auto"` | `SchemaShapes.csproj` `<OpenApi>` item |

## Generated DTOs worth inspecting

| Generated type | What makes it interesting |
|----------------|--------------------------|
| `AccountRequest` | Omits `id` and `createdAt` (readOnly fields) |
| `AccountResponse` | Omits `password` (writeOnly field) |
| `AccountRole` | C# `enum` with `[JsonStringEnumConverter]` |
| `Product` | Neutral record (no readOnly/writeOnly in that schema) |
| `ProductDimensions` | Generated from the nested inline `dimensions` object |
| `ProductAttributesValue` | Generated from the inline object in `additionalProperties` |
| `ProductStatus` | C# `enum` with `[JsonStringEnumConverter]` |
| `CatalogDetail` | Flattened from `allOf` — has both `Catalog` and extension properties |

## How to run

```shell
cd sample/SchemaShapes
dotnet run
```

Then try:

```shell
# Create an account (password is writeOnly — not in the response)
curl -X POST http://localhost:5000/accounts \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"secret","role":"member","profileUrl":null}'

# Create a product (demonstrates DateOnly, enums, additionalProperties)
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Widget","status":"active","availableFrom":"2024-01-01"}'

# Get a catalog (demonstrates allOf flattening)
curl http://localhost:5000/catalogs/00000000-0000-0000-0000-000000000001
```

---

← [Back to sample catalog](../README.md)
