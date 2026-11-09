# Generated contracts and handler implementation

After adding or changing an `<OpenApi>` item, run `dotnet build`; inspect the emitted `<OperationId>EndpointBase` and its exact `HandleAsync` override. Generated namespaces are `{RootNamespace}.{SpecName}.Contracts` and `{RootNamespace}.{SpecName}.Endpoints`. Use the actual emitted signature and namespace; never infer them solely from an OpenAPI name.

## Choose schema ownership by intended coupling

For **new API design**, prefer inline request/response schemas beneath the operation's media-type `schema` when an endpoint's transport contract should evolve independently. This includes endpoint-specific projections, commands, envelopes, filters, and response wrappers. Use `components/schemas` for schemas whose identity and evolution are **intentionally shared**: business concepts such as a value object **or** shared transport contracts that multiple operations deliberately commit to maintain together.

Semantic ownership and planned coupling matter more than whether the schema is called *business* or *transport*. Identical wire shapes alone do not justify sharing; conversely, a deliberately shared transport schema is valid. **When migrating**, preserve existing named component schemas and intended reuse unless there is an explicit, compatibility-reviewed redesign. Do not mechanically inline an established component because it represents a transport DTO.

Inline schemas can contain **nested object properties**, inline arrays of objects, and deeper nested combinations: these generate additional typed records rather than falling back to `object` or `object[]` for supported JSON shapes.

```yaml
paths:
  /orders:
    post:
      operationId: createOrder
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [customer]
              properties:
                customer:
                  type: object
                  required: [name]
                  properties:
                    name:
                      type: string
                lines:
                  type: array
                  items:
                    type: object
                    required: [sku, quantity]
                    properties:
                      sku:
                        type: string
                      quantity:
                        type: integer
      responses:
        "201":
          description: Created
          content:
            application/json:
              schema:
                type: object
                properties:
                  orderId:
                    type: string
                    format: uuid
```

Generated names are scoped and deterministic, but **inspect generated C# before coding against them**; do not invent nested type names. Both shared business concepts and intentionally shared transport schemas may be referenced from inline operation shapes. Do not extract components merely to deduplicate fields, or inline existing components solely to enforce a style preference. Preserve intentional reuse during migrations.

This recommendation concerns **JSON** request/response schemas. Multipart form arrays of objects and dictionaries remain unsupported (MOA011), even though JSON inline arrays of objects work.

Typical mappings:
- Path parameters become direct handler parameters; query, header, and cookie parameters appear in nested `Parameters` records. Cookie binding needs application-specific access.
- Inline body objects become nested `Request` records; component object schemas become contracts; string enums become C# enums; `allOf` object branches flatten.
- `readOnly` / `writeOnly` may yield separate `*Request` and `*Response` contracts.
- JSON and `application/problem+json` responses yield status-specific result types. A single distinct effective type yields one result, multiple yield `Results<...>`.
- `multipart/form-data` binary strings become `IFormFile`; binary arrays become `IReadOnlyList<IFormFile>`; nested form objects bind via dotted keys. Object arrays and dictionaries in forms are unsupported (MOA011).
- `additionalProperties` becomes `Dictionary<string, T>`; component keys are normalized to C# identifiers. Keep `$ref` using the original OpenAPI key.

One concrete handler per operation, and at most one concrete `<OperationId>EndpointConfigurationBase` class. Never call base `HandleAsync` (it throws) or manually map the generated operation.

To inspect emitted files, enable Roslyn emission using `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath` as described in [the upstream README](https://github.com/Kralizek/MinimalOpenApi/blob/master/README.md#inspecting-generated-source). Exclude emitted files from `Compile` to avoid compiling duplicates.

ASP.NET Core does not automatically execute generated `DataAnnotations` validation. Configure validation, antiforgery, request limits, authentication, and storage policies in application code.
