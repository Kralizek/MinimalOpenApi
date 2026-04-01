## Summary

Map `format: date` string schemas to `DateOnly` instead of `string`, gated behind an opt-in MSBuild property to avoid breaking existing consumers.

## Motivation

`format: date` (date without time) maps perfectly to `DateOnly` (.NET 6+), which ASP.NET Core 10 fully supports for model binding. Keeping it as `string` loses compile-time safety.

## Proposed change

In `TypeMapper.MapSchema`:

```csharp
// Before
("string", "date") => "string"

// After (when opt-in is active)
("string", "date") => "global::System.DateOnly"
```

Gate the change behind a new MSBuild property:

```xml
<OpenApiUseDateOnly>true</OpenApiUseDateOnly>
```

## Affected components

- `MinimalOpenAPI` — `TypeMapper`
- `MinimalOpenAPI` — `.targets` file (read MSBuild property, pass to generator)

## ⚠️ Breaking change

This is a **breaking change** for consumers who currently rely on `string` for date fields. The opt-in MSBuild property ensures it cannot be activated silently. A clear migration note must accompany the release.

## Notes

Documented in [`docs/schema-feature-roadmap.md` §4.8](docs/schema-feature-roadmap.md).