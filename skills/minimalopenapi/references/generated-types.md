# Generated contracts and handler implementation

After adding or changing an `<OpenApi>` item, run `dotnet build`; inspect the emitted `<OperationId>EndpointBase` and its exact `HandleAsync` override. Generated namespaces are `{RootNamespace}.{SpecName}.Contracts` and `{RootNamespace}.{SpecName}.Endpoints`. Use the actual emitted signature and namespace; never infer them solely from an OpenAPI name.

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
