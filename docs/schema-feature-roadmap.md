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
| `format` (`uuid`, `date-time`, `date`, `int64`, `float`, …) | ✅ |
| `nullable` | ✅ |
| `$ref` | ✅ |
| `items` (array element type) | ✅ |
| `properties` (object properties) | ✅ |
| `required` (required property list) | ✅ |
| `enum` | ✅ |
| `default` | ❌ |
| `minLength` / `maxLength` | ✅ |
| `minimum` / `maximum` | ✅ |
| `pattern` | ✅ |
| `minItems` / `maxItems` | ✅ |
| `deprecated` | ❌ |
| `readOnly` / `writeOnly` | ❌ |
| `additionalProperties` | ✅ |
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

### Multi-spec and versioning

| Feature | Supported |
|---------|-----------|
| Multiple `<OpenApi>` items per project | ✅ |
| OpenAPI 3.0 | ✅ |
| OpenAPI 3.1 | ✅ |
| Spec publishing (`<OpenApi Publish="true" />`) | ✅ |
| HTTP schema serving (`MapOpenApiSchemas()`) | ✅ |

---

## 2. Categorised analysis

### 2.1 Good near-term candidates

These features have a clear, clean mapping to C# constructs, require no
external packages, and provide immediate consumer value.

#### A. ~~Enum support~~ ✅ Implemented

#### B. ~~Validation attributes (constraint keywords)~~ ✅ Implemented

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

#### D. ~~Nested inline object schemas in DTO properties~~ ✅ Implemented

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

#### F. ~~`additionalProperties` → `Dictionary<string, T>`~~ ✅ Implemented

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

#### I. ~~`format: date` → `DateOnly`~~ ✅ Implemented

#### J. `format: email` / `format: uri` → annotations

`format: email` and `format: uri` on string schemas can generate
`[EmailAddress]` and `[Url]` data annotation attributes respectively, adding
validation metadata without changing the property type (`string`).

- **Scope**: small addition to the annotation-emitting logic.

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

---

## 3. Prioritised recommendation list

The following order balances consumer value, implementation cost, and risk.

| Priority | Feature | Section | Effort |
|----------|---------|---------|--------|
| 1 | ~~Enum support~~ ✅ | §2.1.A | — |
| 2 | ~~Constraint validation attributes~~ ✅ | §2.1.B | — |
| 3 | `deprecated` → `[Obsolete]` | §2.1.C | Small |
| 4 | ~~Nested inline object schemas in DTO properties~~ ✅ | §2.1.D | — |
| 5 | Parameter `default` values | §2.1.E | Small |
| 6 | ~~`additionalProperties` → `Dictionary<string, T>`~~ ✅ | §2.2.F | — |
| 7 | ~~`format: date` → `DateOnly`~~ ✅ | §2.2.I | — |
| 8 | `format: email` / `format: uri` → annotation | §2.2.J | Small |
| 9 | `allOf` flattening | §2.4.K | Large |
| 10 | `readOnly` / `writeOnly` property filtering | §2.2.G | Small |
| — | Problem Details detection | §2.4.M | Design first |
| — | `oneOf` / `anyOf` | §2.4.K | Design first |
| — | Security requirements → authorization | §2.4.L | Design first |
