# Build diagnostics and corrective action

Run `dotnet build` after modifying the OpenAPI contract or MSBuild metadata. Do not paper over generator errors by writing competing manual DTOs or endpoints.

| Diagnostic | Meaning / next check |
| --- | --- |
| MOA001 | Missing concrete handler: implement the generated endpoint base. |
| MOA002 | Multiple concrete handlers: keep one implementation per operation. |
| MOA003 | Multiple concrete endpoint configurations: keep one. |
| MOA004 | OpenAPI parse error: validate YAML/JSON and contract. |
| MOA008 | Invalid/unresolved parameter `$ref`: check `components/parameters`. |
| MOA009 | Duplicate generated document namespace: use distinct `Namespace` metadata. |
| MOA010 | Invalid read/write setting: choose `Ignore`, `Auto`, or `Split`. |
| MOA011 | Unsupported multipart form shape: avoid arrays of objects or dictionaries. |
| MOA012–MOA014 | Generated-name collision: correct conflicting OpenAPI identifiers. |
| MOA015 | Invalid handler-response item: verify `OpenApiFile`, operation `Include`, declared status codes and precedence. |
| MOA016 | Duplicate `operationId`: assign unique operation IDs in the contract. |
| MOA017 | Unresolved/unsupported schema `$ref`: use supported local component references and fix the contract. |

Other MOA diagnostics are documented in the [architecture guide](../../../docs/architecture.md); this quick table intentionally focuses on consumer actions rather than guessing remaining codes. Missing types often mean a failed generator run, a wrong document path or namespace, or a signature changed after regeneration.

Validate with `dotnet build --warnaserror`, application tests and endpoint smoke tests. Verify response status and body as well as the generated C# signature.
