# Generated contracts and handler implementation

After adding or changing an `<OpenApi>` item, run `dotnet build`; inspect the emitted `<OperationId>EndpointBase` and its exact `HandleAsync` override. Generated namespaces are `{RootNamespace}.{SpecName}.Contracts` and `{RootNamespace}.{SpecName}.Endpoints`. Use the actual emitted signature and namespace; never infer them solely from an OpenAPI name.

## Distinguish business schemas from transport schemas

Use `components/schemas` for **shared business-level concepts** whose identity and semantics intentionally span API operations (for example, a domain entity or value object). Declare **transport-only request and response shapes inline**, directly beneath the operation's media-type `schema`. This includes endpoint-specific projections, commands, envelopes, filters, and response wrappers, even when two operations currently expose identical properties.

The deciding factor is **semantic ownership**, not the number of references or whether two schemas happen to look alike. Reusing an endpoint-specific DTO through `components/schemas` couples otherwise independent endpoints: changing one endpoint's wire contract can accidentally force a change to another. Keep those shapes inline so each operation can evolve independently. Conversely, use a shared component when the contract truly represents the *same business concept*, whose shared evolution is intentional.

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

Generated names are scoped and deterministic, but **inspect generated C# before coding against them**; do not invent nested type names. A shared business concept may be referenced from several inline transport shapes. Do not extract transport DTOs into global components merely to deduplicate fields, and do not inline a genuine shared business concept simply to avoid a `$ref`.

This recommendation concerns **JSON** request/response schemas. Multipart form arrays of objects and dictionaries remain unsupported (MOA011), even though JSON inline arrays of objects work.

Typical mappings:
- Path parameters become direct handler parameters; query, header, and cookie parameters appear in nested `Parameters` records. Cookie binding needs application-specific access.
- Inline body objects become nested `Request` records; component object schemas become contracts; string enums become C# enums; `allOf` object branches flatten.
- `readOnly` / `writeOnly` may yield separate `*Request` and `*Response` contracts.
- JSON and `application/problem+json` responses yield status-specific result types. A single distinct effective type yields one result, multiple yield `Results<...>`.
- `multipart/form-data` binary strings become `IFormFile`; binary arrays become `IReadOnlyList<IFormFile>`; nested form objects bind via dotted keys. Object arrays and dictionaries in forms are unsupported (MOA011).
- `additionalProperties` becomes `Dictionary<string, T>`; component keys are normalized to C# identifiers. Keep `$ref` using the original OpenAPI key.

One concrete handler per operation, and at most one concrete `<OperationId>EndpointConfigurationBase` class. Never call base `HandleAsync` (it throws) or manually map the generated operation.

To inspect emitted files, enable Roslyn emission using `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath` as described in [the README](../../../README.md#inspecting-generated-source). Exclude emitted files from `Compile` to avoid compiling duplicates.

ASP.NET Core does not automatically execute generated `DataAnnotations` validation. Configure validation, antiforgery, request limits, authentication, and storage policies in application code.
