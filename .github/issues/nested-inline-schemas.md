## Summary

When a component schema has a property whose schema is itself an inline object (no `$ref`), generate a named record for it instead of falling back to `object`.

## Motivation

Currently `TypeMapper.MapSchema` resolves inline object schemas as `object`, losing all type information. A spec like:

```yaml
components:
  schemas:
    Order:
      type: object
      properties:
        address:
          type: object
          properties:
            street: { type: string }
            city:   { type: string }
```

should produce an `OrderAddress` record rather than `object Address`.

## Proposed implementation

Documented in [`docs/schema-feature-roadmap.md` §4.4](docs/schema-feature-roadmap.md).

1. Pre-scan each record's properties for inline object schemas.
2. Assign a derived name: `{RecordName}{PascalCase(PropertyName)}` emitted as a **top-level** record in the Contracts namespace.
3. Emit nested types before the parent record.
4. Pass a resolver delegate to `TypeMapper.MapSchema`.

## Affected components

- `MinimalOpenAPI` — `DtoGenerator`, `TypeMapper`

## Notes

- Must handle **recursive** inline objects with cycle detection.
- Name conflicts (two sibling inline-object properties with the same derived name) need a documented resolution strategy.