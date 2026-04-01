## Summary

Support the `oneOf` and `anyOf` composition keywords for discriminated and open union schemas.

## Motivation

`oneOf`/`anyOf` are used to model polymorphic payloads (e.g. different event types, heterogeneous response bodies). Without support, these schemas produce `object`, losing all type safety.

## Design options (must be chosen before implementation)

1. **Base record + derived records** — mirrors the JSON Schema intent but C# records do not naturally support polymorphism without `abstract`.
2. **`[JsonDerivedType]`** (System.Text.Json 7+) — works for deserialization but requires a discriminator field in the spec.
3. **Fall back to `object`** — safe but loses type information.

## Notes

- Implement `allOf` flattening first (#see allOf issue); `oneOf`/`anyOf` is a larger, separate effort.
- This issue tracks **design and implementation** of `oneOf`/`anyOf` support.
- Documented in [`docs/schema-feature-roadmap.md` §2.4.K](docs/schema-feature-roadmap.md). **Design must be agreed before implementation begins.**