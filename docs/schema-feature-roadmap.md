# OpenAPI Feature Support and Roadmap

This document records the supported OpenAPI surface and features intentionally deferred. The schema table distinguishes additions since 1.0.

The OpenAPI document is the source of truth. MinimalOpenAPI focuses on features that can be represented predictably in generated C# contracts, ASP.NET Core Minimal API binding, typed results, and endpoint metadata.

## Document support

| Feature | 1.0 status | Notes |
|---|---|---|
| OpenAPI 3.0 | Supported | YAML and JSON. |
| OpenAPI 3.1 | Supported | YAML and JSON; nullable type arrays are normalized. |
| OpenAPI 2.0 | Not supported | Convert the document to OpenAPI 3.x. |
| Multiple documents | Supported | Each document gets an isolated generated namespace. |
| Authored document publishing | Supported | Every document is copied to output; `PublishAs` opts into HTTP exposure. |
| Runtime document generation | Non-goal | MinimalOpenAPI serves authored documents instead of generating a second document from C#. |

## Schema support

| Feature | Status | Notes |
|---|---|---|
| Primitive types and formats | Supported | Includes strings, booleans, integers, numbers, `uuid`, `date-time`, and `date`. |
| Object schemas | Supported | Component and inline object records. |
| Nested inline objects | Supported | Deterministic sibling record generation. |
| Inline objects as array items | Supported | Recursive collection through nested object and array boundaries. |
| Arrays | Supported | Including nested arrays and generated inline item types. |
| String enums | Supported | Generated C# enums use `JsonStringEnumConverter`. |
| `additionalProperties` | Supported | Maps to `Dictionary<string, T>`; inline object values generate records. |
| `allOf` | Supported | Object branches are flattened into one generated shape. Incompatible properties produce `MOA007` and fall back to `JsonElement`. |
| `oneOf` / `anyOf` | Added after 1.0 | Native C# 15 JSON unions with System.Text.Json 11. Both keywords use the same CLR representation; neither enforces JSON Schema match counts. |
| `nullable` | Supported | OpenAPI 3.0 nullable and OpenAPI 3.1 type-array forms. |
| `required` | Supported | Drives required members and nullability. |
| `readOnly` / `writeOnly` | Supported | Controlled by `ReadWriteSchemaHandling="Ignore|Auto|Split"`. |
| `default` on operation parameters | Supported | Generates initializers for supported scalar formats. |
| `default` on DTO properties | Not generated | Optional DTO properties already express absence through nullability. |
| String constraints | Supported | `minLength`, `maxLength`, and `pattern`. |
| Numeric constraints | Supported | `minimum` and `maximum`. |
| Array constraints | Supported | `minItems` and `maxItems`. |
| `format: email` | Supported | Emits `[EmailAddress]`. |
| `format: uri` | Supported | Emits `[Url]`. |
| `deprecated` | Deferred | Candidate mapping to `[Obsolete]`; operation behavior and messages need design. |
| `example` / `examples` | Not generated | Documentation-only metadata is better handled by the authored OpenAPI document and UI tooling. |

## Schema names

All `components/schemas` keys are normalized through one document-level `SchemaNameMap` before code generation.

- separator characters such as `.`, `-`, and `_` create PascalCase word boundaries;
- names beginning with a digit receive a `Value` prefix;
- the original OpenAPI key remains the lookup identity for `$ref` resolution;
- normalized component-name collisions produce `MOA012`;
- names with no usable identifier characters produce `MOA013`;
- collisions involving generated scoped or inline symbols produce `MOA014`.

Generation is aborted for an affected document rather than emitting ambiguous or invalid C#.

## Parameters

| Feature | 1.0 status | Notes |
|---|---|---|
| Path parameters | Supported | Typed and emitted with route constraints where available. |
| Query parameters | Supported | Bound through generated `Parameters` records. |
| Header parameters | Supported | Original header names are preserved with `[FromHeader(Name = ...)]`. |
| Cookie parameters | Partial | Surfaced in the parameter model, but ASP.NET Core has no built-in `[FromCookie]` binding attribute. Applications may use `HttpContext`. |
| Reusable component parameters | Supported | `$ref` values are resolved; failures produce `MOA008`. |
| Path-level parameters | Supported | Operation-level declarations override matching path-level parameters. |
| Parameter defaults | Supported | String, boolean, numeric, UUID, and date defaults are emitted where representable. |
| Parameter `style` / `explode` | Not generated | ASP.NET Core binding does not expose an equivalent declarative surface for all OpenAPI serialization styles. |

## Request bodies

| Feature | 1.0 status | Notes |
|---|---|---|
| `application/json` | Supported | Component and inline schemas produce typed handler parameters. |
| `multipart/form-data` | Supported | Generates form-bound request records. |
| Binary file fields | Supported | `type: string`, `format: binary` maps to `IFormFile`. |
| Multiple files | Supported | Arrays of binary strings map to `IReadOnlyList<IFormFile>`. |
| Nested form objects | Supported | ASP.NET Core dotted keys such as `metadata.title`. |
| Form arrays of objects | Not supported | Produces `MOA011`; generated model binding cannot represent the shape reliably. |
| Form dictionaries | Not supported | Produces `MOA011`. |
| Other request media types | Deferred | Media types are preserved in the parsed model so support can be added deliberately. |
| Multiple request media types | Limited | JSON is preferred when both JSON and multipart are present. A broader selection model is deferred. |

Antiforgery, request-size limits, file validation, and storage remain application policies. Configure them through ASP.NET Core and the generated endpoint configuration.

## Responses

| Feature | 1.0 status | Notes |
|---|---|---|
| JSON response bodies | Supported | Typed results use generated component or inline contracts. |
| Multiple status codes | Supported | Produces `Results<T1, T2, ...>` unions. |
| Empty responses | Supported | Maps to status-specific no-content result types where applicable. |
| `application/problem+json` | Supported | Produces status-specific typed wrappers around the declared problem payload. |
| Non-JSON text responses | Deferred | Requires a deliberate mapping to content results. Responses that declare only unsupported media types report `MOA018`. |
| Binary/file responses | Supported | `application/octet-stream`, `application/pdf`, `image/*`, `audio/*`, `video/*`, and other non-JSON media types with a `string`/`binary` schema produce status-specific file wrappers such as `OkFile`. Wildcard media types require the handler to supply a validated concrete content type. |
| Multiple response media types | Deferred | Needs deterministic result-type and metadata rules. |
| Response headers | Not generated | Applications set headers through `HttpContext`; no useful strongly typed Minimal API equivalent exists. |

## Operation metadata and policies

| Feature | 1.0 status | Notes |
|---|---|---|
| `operationId` | Required for generated operations | Drives handler and endpoint configuration type names. |
| Summary and description | Supported | Emitted as endpoint metadata. |
| Tags | Supported | Emitted through endpoint metadata. |
| Security requirements | Policy hook | Authorization is application-specific; configure it through `<OperationId>EndpointConfigurationBase`. |
| Rate limiting | Policy hook | Configure through the endpoint configuration. |
| Antiforgery | Policy hook | Configure globally or per endpoint. |
| Request-size limits | Policy hook | Configure through standard ASP.NET Core endpoint conventions. |

## Schema union boundaries

`oneOf` and `anyOf` support compatible scalar alternatives and local or inline object cases with distinguishable required properties. Discriminators need explicit mappings to local referenced object alternatives and a string discriminator property on every case. The union can itself be inline or a component, including in JSON bodies, object properties, and array items. Directional case contracts follow `Ignore`, `Auto`, and `Split`.

MOA019 reports unsafe or unsupported representations: overlapping token categories (including string plus number), mixed scalar/object cases, enum cases, nullable unions/cases, a union used directly as a case, conflicting composition, and uses outside JSON payloads. A `$ref` must name a generated union or object rather than a union-containing array or dictionary alias. In a top-level body array or dictionary, use a named object for union-bearing object items. The generator does not rewrite the authored contract or add wrapper objects. See the [README](../README.md#schema-unions) for .NET 10/11 requirements and JSON metadata limitations.

## Post-1.0 priorities

The likely next design areas are:

1. Non-JSON text response families.
2. Additional request media types where ASP.NET Core has a stable, strongly typed binding model.
3. `deprecated` metadata mapped consistently to generated C# and endpoint metadata.
4. Broader parameter serialization support where it can be represented without custom runtime binders.

Each addition must preserve deterministic generated names, explicit diagnostics for unsupported shapes, and Semantic Versioning compatibility for both public runtime APIs and generated consumer code.
