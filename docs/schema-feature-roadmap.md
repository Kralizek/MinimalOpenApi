# MinimalOpenAPI — OpenAPI Schema Feature Roadmap

> **Audience**: contributors and maintainers deciding what to build next.
> This document analyses the OpenAPI 3.0.x specification surface and makes
> concrete recommendations for what MinimalOpenAPI should support in the
> near, medium, and long term.

---

## 1. Baseline — what is already supported

Before analysing gaps, it is useful to document what the current implementation
already covers.

### Schema model (`OpenApiSchema`)

| Field | Supported |
|-------|-----------|
| `type` | ✅ |
| `format` (`uuid`, `date-time`, `int64`, `float`, …) | ✅ |
| `nullable` | ✅ |
| `$ref` | ✅ |
| `items` (array element type) | ✅ |
| `properties` (object properties) | ✅ |
| `required` (required property list) | ✅ |
| `enum` | ❌ |
| `default` | ❌ |
| `minLength` / `maxLength` | ✅ |
| `minimum` / `maximum` | ✅ |
| `pattern` | ✅ |
| `minItems` / `maxItems` | ✅ |
| `deprecated` | ❌ |
| `readOnly` / `writeOnly` | ❌ |
| `additionalProperties` | ❌ |
| `allOf` / `oneOf` / `anyOf` | ❌ |

### Operation model (`OpenApiOperation`)

| Field | Supported |
|-------|-----------|
| `operationId` | ✅ |
| `summary` → `.WithSummary()` | ✅ |
| `description` → `.WithDescription()` | ✅ |
| `tags` → `.WithTags()` | ✅ |
| `deprecated` | ❌ |
| `security` | ❌ |

### Parameters

| Feature | Supported |
|---------|-----------|
| Path parameters (typed, route-constrained) | ✅ |
| Query parameters (`[FromQuery]`) | ✅ |
| Header parameters (`[FromHeader]`) | ✅ |
| Cookie parameters (in `Parameters` record, no auto-binding) | ⚠️ partial |
| Parameter `required` / optional | ✅ |
| Parameter `default` value | ❌ |

### Request / response

| Feature | Supported |
|---------|-----------|
| `application/json` request body | ✅ |
| `$ref` and inline object request schemas | ✅ |
| `$ref` and inline object response schemas | ✅ |
| Typed results (`Ok<T>`, `NotFound`, …) | ✅ |
| `.Produces<T>()` metadata | ✅ |
| Inline nested records inside handler base classes | ✅ |
| Response headers | ❌ |
| Multiple content types per operation | ❌ |
| `application/problem+json` detection | ❌ |

---

## 2. Categorised analysis

### 2.1 Good near-term candidates

These features have a clear, clean mapping to C# constructs, require no
external packages, and provide immediate consumer value.

#### A. Enum support

**OpenAPI keyword**: `enum` on a schema.

Maps cleanly to a C# `enum` type at the DTO and parameter level.  The
generated C# enum can have `[JsonStringEnumConverter]` applied for
serialisation.

- **Scope**: `OpenApiSchema.Enum` list; `DtoGenerator` emits top-level enums;
  `TypeMapper.MapSchema` maps an enum schema to its generated enum name.
- **Pitfall**: OpenAPI enums are values, not named types; the generator must
  derive a name from context (schema name, property name, or parameter name).
  Inline enum schemas on parameters or properties need a stable naming
  convention.
- **Validation**: no extra validation needed — the enum type itself enforces
  the constraint at bind time.

#### B. Validation attributes (constraint keywords)

**OpenAPI keywords**: `minLength`, `maxLength`, `pattern`, `minimum`,
`maximum`, `minItems`, `maxItems`.

These map directly to `System.ComponentModel.DataAnnotations` attributes that
ASP.NET Core respects during model binding:

| OpenAPI keyword | C# attribute |
|-----------------|-------------|
| `minLength` / `maxLength` on `string` | `[StringLength(max, MinimumLength = min)]` or `[MinLength]` / `[MaxLength]` |
| `pattern` | `[RegularExpression(pattern)]` |
| `minimum` / `maximum` on `integer` / `number` | `[Range(min, max)]` |
| `minItems` / `maxItems` on `array` | `[MinLength]` / `[MaxLength]` |

No external validation library is required — `DataAnnotations` validation is
built into ASP.NET Core.  Constraints apply to DTO properties and `Parameters`
record properties.

- **Scope**: `OpenApiSchema` gains numeric/string constraint fields; `DtoGenerator`
  and the `Parameters` record emitter apply the relevant attributes.
- **Note**: ASP.NET Core minimal APIs do not run `DataAnnotations` validation
  automatically; consumers must call `app.UseDataAnnotationsValidation()` or use
  the built-in filter.  The generated attributes still provide OpenAPI metadata
  and IDE-level hints even without runtime enforcement.

#### C. `deprecated` → `[Obsolete]`

**OpenAPI keywords**: `deprecated` on a schema property, operation, or
parameter.

Maps cleanly to the `[Obsolete]` attribute.  Applied to:

- Generated DTO record properties (when `deprecated: true` on a property).
- The handler base class (when `deprecated: true` on an operation).
- Individual properties of the `Parameters` record (when `deprecated: true` on
  a parameter).

- **Scope**: `OpenApiSchema` gains a `Deprecated` bool; `OpenApiOperation` gains
  a `Deprecated` bool; `DtoGenerator`, `HandlerBaseGenerator`, and the
  Parameters emitter apply `[Obsolete]`.
- **Value**: low implementation cost, high signal quality for consumers.

#### D. Nested inline object schemas in DTO properties

**Situation**: a component schema has a property whose schema is itself an
inline object (no `$ref`).  Currently `TypeMapper.MapSchema` falls through to
`object` for such schemas when no `InlineSchemaResolver` is provided.

Generated code for:

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

should emit a nested record `Order.Address` rather than `object`.

- **Scope**: `DtoGenerator` must recursively emit nested records and use their
  short names in the parent record.
- **Pitfall**: name conflicts if two sibling properties are both inline objects
  with the same shape; naming must be derived from the property name.

#### E. Parameter `default` values

**OpenAPI keyword**: `default` on a parameter schema.

Maps to a C# default parameter value or a property initialiser in the
`Parameters` record.

```yaml
parameters:
  - name: pageSize
    in: query
    schema:
      type: integer
      default: 20
```

Generated:

```csharp
[FromQuery(Name = "pageSize")]
public int PageSize { get; init; } = 20;
```

- **Scope**: `OpenApiSchema` gains a `Default` string/object field; parser reads
  it; the Parameters record emitter writes `= <value>` initialisers.
- **Note**: `default` on a DTO property schema is less commonly useful since
  clients are expected to supply all `required` fields and optional fields
  already get `null` defaults via nullability.

---

### 2.2 Possible later candidates

These features fit the framework's philosophy but require more design thought
or have lower priority because they cover edge cases.

#### F. `additionalProperties` → `Dictionary<string, T>`

**OpenAPI keyword**: `additionalProperties` on an object schema.

Maps to `Dictionary<string, T>` where `T` is the value schema type.

```yaml
labels:
  type: object
  additionalProperties:
    type: string
```

Generated: `Dictionary<string, string> Labels`.

- **Pitfall**: `additionalProperties: true` (allow any extra property) is
  ambiguous.  Only `additionalProperties: { schema }` can be mapped
  deterministically.

#### G. `readOnly` and `writeOnly` properties

**OpenAPI keywords**: `readOnly`, `writeOnly` on a schema property.

In a request body schema a `readOnly: true` property should be omitted; in a
response schema a `writeOnly: true` property should be omitted.  Since
MinimalOpenAPI generates separate request and response types this is a natural
fit.

- **Scope**: `OpenApiSchema` gains `ReadOnly`/`WriteOnly` bools; the generators
  filter properties when building request vs response records.
- **Note**: these keywords are advisory in OpenAPI 3.0; ignoring them is
  technically conformant.  Low priority unless consumers request it.

#### H. Header parameters (full binding)

Header parameters already appear in the generated `Parameters` record with
`[FromHeader]`.  The gap is that the parser does not currently treat header
names in a case-insensitive way as HTTP headers require, and the route
constraint builder ignores them.  Closing this gap is a refinement rather than
a new feature.

#### I. `format: date` → `DateOnly`

Currently `format: date` maps to `string`.  .NET 6+ has `DateOnly` and
ASP.NET Core 10 supports its binding out of the box.  Mapping `format: date`
to `DateOnly` is a clean improvement.

- **Scope**: one-line change in `TypeMapper.MapSchema`, but gated behind an
  MSBuild property (`<OpenApiUseDateOnly>true</OpenApiUseDateOnly>`) or
  introduced in a minor version bump with a clear migration note.
- **Risk**: breaking change for consumers who expect `string` today; must not
  be made silently.

#### J. `format: email` / `format: uri` → annotations

`format: email` and `format: uri` on string schemas can generate
`[EmailAddress]` and `[Url]` data annotation attributes respectively, adding
validation metadata without changing the property type (`string`).

- **Scope**: small addition to the annotation-emitting logic (see §2.1.B).

---

### 2.3 Probably not worth supporting

These features are part of the OpenAPI specification but are a poor fit for
MinimalOpenAPI's contract-first, code-generation philosophy, have no clean
mapping to ASP.NET Core Minimal APIs, or would deliver little practical value.

#### Cookie parameters (auto-binding)

ASP.NET Core Minimal APIs do not provide a `[FromCookie]` attribute.  Cookies
must be accessed manually via `HttpContext.Request.Cookies`.  Cookie parameters
are already placed in the generated `Parameters` record so their existence is
signalled to the handler, but auto-binding is not feasible without custom model
binders.  **Recommendation**: keep as-is with the existing commentary.

#### Response headers

OpenAPI allows operations to declare response headers (e.g.
`X-RateLimit-Remaining`).  There is no equivalent typed construct in ASP.NET
Core Minimal APIs; headers must be set imperatively via `HttpContext.Response.Headers`.
Adding metadata annotations for response headers would be pure documentation
with no binding benefit.

#### Multiple content types per operation

MinimalOpenAPI reads the first `application/json` content schema and ignores
other content types.  Supporting `multipart/form-data`, `text/plain`, or
`application/octet-stream` would require fundamentally different handler
signatures and binding logic.  Out of scope for this framework.

#### `explode` / `style` on parameters

These control how arrays and objects are serialised in query strings (e.g.
`color=blue,black` vs `color=blue&color=black`).  ASP.NET Core handles this at
the serialisation layer and does not expose it as a binding attribute.
Generated code cannot express these options.

#### `example` / `examples` on schemas and operations

Useful for documentation tooling but has no C# code generation value.  If
consumers want Swagger UI examples, they should use a dedicated OpenAPI serving
tool (Scalar, Swashbuckle) alongside MinimalOpenAPI.

#### `externalDocs`

Documentation-only.  No code generation value.

#### OpenAPI 3.1 JSON Schema dialect

OpenAPI 3.1 changes the schema model significantly (e.g. `nullable` is
replaced by `null` in the `type` array).  Supporting 3.1 is a separate,
sizeable effort and should be tracked as its own feature.

---

### 2.4 Needs custom design / careful constraints

These features would require non-trivial design decisions before implementation
begins.

#### K. Schema composition — `allOf`, `oneOf`, `anyOf`

**`allOf`** (all subschemas must validate): conceptually equivalent to
merging properties from multiple schemas.  A reasonable strategy is
**property flattening**: collect all properties from each subschema and
emit them in a single record.  Conflicts (same property name, different
types) need a resolution strategy.

**`oneOf` / `anyOf`** (one or any subschema must validate): these are
discriminated or open unions with no direct C# equivalent.  Possible
approaches:

1. **Emit a base record + derived records**: mirrors the JSON Schema intent but
   C# records do not naturally support polymorphism without `abstract`.
2. **Use `[JsonDerivedType]`** (System.Text.Json 7+): works for deserialization
   but requires a discriminator field in the spec.
3. **Fall back to `object`**: safe but loses type information.

**Recommendation**: implement `allOf` flattening first (high value, bounded
design space), then design `oneOf`/`anyOf` as a separate effort based on
consumer demand.

#### L. Security requirements → `[Authorize]`

```yaml
security:
  - bearerAuth: []
```

Mapping security requirements to `[Authorize]` attributes or `.RequireAuthorization()`
calls is technically feasible, but the mapping from scheme name to policy
name is application-specific.  Options:

1. Always emit `.RequireAuthorization()` when any security requirement is
   present (coarse-grained).
2. Emit `.RequireAuthorization(schemeName)` and let the consumer configure a
   matching policy (fine-grained but fragile without spec conventions).
3. Expose a customisation hook via `EndpointRegistration` (preferred — keeps
   the library policy-agnostic).

**Recommendation**: do not auto-generate security attributes; instead, document
the customisation hook pattern for consumers who need it.

#### M. Problem Details response detection

When a response declares `content: application/problem+json`, the response body
is conventionally a `ProblemDetails` object (RFC 7807 / RFC 9457).  ASP.NET
Core has `Microsoft.AspNetCore.Http.ProblemDetails` built in.

The generator could detect `application/problem+json` content type in a
response and substitute `ProblemDetails` as the schema type instead of emitting
a custom DTO.  This avoids cluttering the generated contracts namespace with
redundant error DTOs.

- **Design question**: should this be opt-in (consumer adds an attribute or
  marker) or automatic (detect content type)?  Automatic detection keeps
  consumer experience simple but may surprise consumers who have a custom
  problem details extension schema.

#### N. Multiple OpenAPI specs per project

The current model assumes one `<OpenApi>` item per project.  Supporting
multiple specs would require namespace isolation (e.g.
`{RootNamespace}.{SpecName}.Contracts` and `{RootNamespace}.{SpecName}.Endpoints`)
to prevent type name collisions.

---

## 3. Prioritised recommendation list

The following order balances consumer value, implementation cost, and risk.

| Priority | Feature | Section | Effort |
|----------|---------|---------|--------|
| 1 | Enum support | §2.1.A | Medium |
| 2 | ~~Constraint validation attributes (`minLength` / `maxLength` / `pattern` / `minimum` / `maximum`)~~ ✅ | §2.1.B | Medium |
| 3 | `deprecated` → `[Obsolete]` | §2.1.C | Small |
| 4 | Nested inline object schemas in DTO properties | §2.1.D | Medium |
| 5 | Parameter `default` values | §2.1.E | Small |
| 6 | `additionalProperties` → `Dictionary<string, T>` | §2.2.F | Small |
| 7 | `format: date` → `DateOnly` | §2.2.I | Trivial (breaking) |
| 8 | `format: email` / `format: uri` → annotation | §2.2.J | Small |
| 9 | `allOf` flattening | §2.4.K | Large |
| 10 | `readOnly` / `writeOnly` property filtering | §2.2.G | Small |
| — | Problem Details detection | §2.4.M | Design first |
| — | `oneOf` / `anyOf` | §2.4.K | Design first |
| — | Security requirements → authorization | §2.4.L | Design first |

---

## 4. Implementation notes

### 4.1 Enum support (Priority 1)

**Affects**: `OpenApiSchema`, both parsers, `DtoGenerator`, `TypeMapper`.

1. Add `List<string>? Enum` to `OpenApiSchema`.
2. Both parsers read the `enum` YAML/JSON array and populate it.
3. `DtoGenerator`: when a component schema has a non-null `Enum` list, emit a
   C# `enum` instead of a record.  Apply `[JsonConverter(typeof(JsonStringEnumConverter))]`
   at the declaration site.
4. `TypeMapper.MapSchema`: when a schema has a non-null `Enum` list and a
   `Reference` is resolvable, map to the generated enum name.
5. Inline enum schemas (on properties or parameters): derive the enum name from
   the containing schema name + property name (e.g. `Order.Status` property
   with `enum: [pending, shipped, delivered]` → emit a top-level
   `OrderStatus` enum in the Contracts namespace, or a nested enum inside the
   parent record).

**No external packages required.** `System.Text.Json` supports
`JsonStringEnumConverter` out of the box.

---

### 4.2 Constraint validation attributes (Priority 2)

**Affects**: `OpenApiSchema`, both parsers, `DtoGenerator`, `HandlerBaseGenerator`
(Parameters record emitter).

1. Add the following nullable fields to `OpenApiSchema`:
   - `int? MinLength`, `int? MaxLength` (strings and arrays)
   - `string? Pattern` (strings)
   - `double? Minimum`, `double? Maximum` (numbers / integers)
   - `int? MinItems`, `int? MaxItems` (arrays)
2. Both parsers read these fields.
3. Create a helper `ValidationAttributeEmitter` (or extend `TypeMapper`) that
   converts the schema fields to `[StringLength]`, `[Range]`, `[RegularExpression]`,
   `[MinLength]`, `[MaxLength]` attribute strings.
4. Apply the helper in `DtoGenerator.GenerateRecord` (for DTO properties) and
   in the Parameters record emitter in `HandlerBaseGenerator`.

**Runtime enforcement**: ASP.NET Core 10 does not run `DataAnnotations`
validation on minimal API parameters by default.  Generated attributes will
appear in OpenAPI metadata (via the endpoint metadata pipeline) and are
respected if the consumer enables filter-based validation.  Document this
clearly in XML comments on the generated properties.

---

### 4.3 `deprecated` → `[Obsolete]` (Priority 3)

**Affects**: `OpenApiSchema`, `OpenApiOperation`, `OpenApiParameter`, both
parsers, `DtoGenerator`, `HandlerBaseGenerator`, `EndpointMappingGenerator`.

1. Add `bool Deprecated` to `OpenApiSchema`, `OpenApiOperation`,
   `OpenApiParameter`.
2. Parsers read the `deprecated` boolean from each object.
3. `DtoGenerator`: emit `[Obsolete]` before deprecated properties.
4. `HandlerBaseGenerator`: emit `[Obsolete]` before the class declaration when
   `operation.Deprecated` is true.
5. `EndpointMappingGenerator`: no change needed — the deprecation is expressed
   on the base class already.

---

### 4.4 Nested inline object schemas in DTO properties (Priority 4)

**Affects**: `DtoGenerator`.

Currently `DtoGenerator.GenerateRecord` calls `TypeMapper.MapSchema` with no
`resolveInline` delegate, so inline object properties resolve to `object`.

The fix is to:

1. Pre-scan each record's properties for inline object schemas.
2. Assign each a derived name: `{RecordName}{PascalCase(PropertyName)}` emitted
   as a **top-level** record in the Contracts namespace (or as a nested record
   inside the parent — top-level is simpler and avoids deeply nested types).
3. Emit the nested type before the parent record.
4. Pass a resolver delegate to `TypeMapper.MapSchema` that maps each inline
   schema to its derived name.

**Pitfall**: recursive inline objects (an inline object whose property is
itself an inline object) require recursive pre-scanning with cycle detection.

---

### 4.5 Parameter `default` values (Priority 5)

**Affects**: `OpenApiSchema`, both parsers, `HandlerBaseGenerator` (Parameters
record emitter).

1. Add `string? Default` to `OpenApiSchema`.  Store it as a raw string so that
   the generator can emit it verbatim or with appropriate C# literal syntax.
2. Parsers read the `default` field.
3. The Parameters record emitter writes `= <CSharpLiteral(default)>` after the
   property type, where `CSharpLiteral` converts the raw string to a valid C#
   expression using `TypeMapper.GetDefaultValue` or a similar helper.

---

### 4.6 `additionalProperties` (Priority 6)

**Affects**: `OpenApiSchema`, both parsers, `DtoGenerator`, `TypeMapper`.

1. Add `OpenApiSchema? AdditionalProperties` to `OpenApiSchema`.
2. Parsers read `additionalProperties`.
3. `TypeMapper.MapSchema`: when `schema.AdditionalProperties is not null` and
   `schema.Properties.Count == 0`, map to
   `Dictionary<string, {MapSchema(schema.AdditionalProperties)}>`.
4. `DtoGenerator`: treat such schemas as dictionary properties rather than
   record properties.

---

### 4.7 `allOf` flattening (Priority 9)

**Affects**: `OpenApiSchema`, both parsers, `DtoGenerator`.

1. Add `List<OpenApiSchema> AllOf` to `OpenApiSchema`.
2. Parsers resolve each subschema in `allOf` (may be `$ref` or inline).
3. `DtoGenerator`: when `schema.AllOf.Count > 0`, flatten all properties from
   all subschemas into a single generated record.  Resolve `$ref` subschemas
   by looking up `allSchemas`.  Required lists are unioned.
4. **Breaking constraint**: if two subschemas declare a property with the same
   name and incompatible types, emit a diagnostic warning (`MOA006`?) and fall
   back to `object` for the conflicting property.

---

### 4.8 Format additions — `date`, `email`, `uri` (Priorities 7–8)

**Affects**: `TypeMapper.MapSchema` only.

- `("string", "date") => "global::System.DateOnly"` — **breaking change**;
  gate behind a new `<OpenApiUseDateOnly>true</OpenApiUseDateOnly>` MSBuild
  property or introduce in a minor version bump with a clear migration note.
- For `email` and `uri`: keep the C# type as `string` but emit
  `[EmailAddress]` / `[Url]` attributes in the property emitters.

---

## 5. What is explicitly out of scope

- **External validation libraries** (FluentValidation, MinimalApiValidator, etc.)
- **Runtime OpenAPI document serving** (Swashbuckle, Scalar, NSwag)
- **OpenAPI 3.1** (tracked separately)
- **Code-first path** (out of scope by design)
- **`explode` / `style` on parameters**
- **Multiple content types per operation**
- **Response headers**
- **Cookie auto-binding**
