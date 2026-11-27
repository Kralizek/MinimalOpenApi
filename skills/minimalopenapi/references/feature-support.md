# OpenAPI feature support and boundaries

Check the [repository feature matrix](https://github.com/Kralizek/MinimalOpenApi/blob/master/docs/schema-feature-roadmap.md) before choosing a construct. The table below summarizes the documented surface; it does **not** imply that an unsupported construct is invalid OpenAPI. These are limitations of **MinimalOpenAPI generation**.

## Unsupported or deferred constructs

| Construct | Guidance for consumers |
| --- | --- |
| OpenAPI 2.0 / Swagger 2.0 | Not supported. Use an OpenAPI 3.0/3.1 contract. |
| Multipart form arrays of objects or dictionaries | Not supported; MOA011. Nested form objects and file arrays **are** supported. |
| Binary/file responses | `application/octet-stream`, `application/pdf`, `image/*`, `audio/*`, `video/*`, or a `string`/`binary` schema generate status-specific file wrappers such as `OkFile`. |
| Non-JSON text responses | Deferred; a response whose media types are all unsupported reports MOA018 and has no generated body. |
| Multiple response media types | Support is deferred; do not assume arbitrary content negotiation will be generated. |
| Other request media types | Deferred. JSON and multipart have explicit support; when both are declared JSON is preferred and selection is limited. |
| Parameter `style` / `explode` | Not generated; application-specific binding may be needed. |
| Response headers | Not generated as typed response contracts; set headers in application code using `HttpContext`. |
| DTO-property `default` | Not generated; operation-parameter scalar defaults are supported. |
| `example` / `examples` | Not generated into C#; keep them in the authored document for documentation tools. |
| `deprecated` | No generated `[Obsolete]` equivalent yet. |

## Partial support and runtime caveats

- On a package version that supports schema unions, `oneOf` and `anyOf` generate native C# 15 unions for JSON. Use the .NET 11 SDK, C# 15, and System.Text.Json 11 (reference it explicitly on `net10.0`). Cases must have distinguishable JSON tokens or distinct required object properties, or be referenced objects with an explicit string discriminator mapping. MOA019 identifies unsupported shapes. Use reflection-based JSON metadata; a `JsonSerializerContext` in the same compilation cannot see generated contracts, and trimming and Native AOT support are not established. This feature selects one CLR case; it does not validate composition match counts or change the authored OpenAPI document.
- Cookie parameters appear in generated parameter models, but ASP.NET Core doesn't automatically bind them; access `HttpContext` or supply application binding.
- Generated `DataAnnotations` attributes do **not** execute validation automatically in ASP.NET Core Minimal APIs; configure validation.
- `allOf` object flattening is supported, but incompatible properties can trigger MOA007 with a `JsonElement` fallback. This is not general union support.
- Security declarations do not configure authentication or authorization for you; apply ASP.NET Core policies using endpoint configuration.

An agent must not quietly discard unsupported OpenAPI semantics, substitute an unrelated construct, or claim support based solely on OpenAPI validity. Explain the constraint, inspect the generator diagnostics, and agree on a contract-preserving path where possible.

For precise details and future changes, consult [the feature support matrix](https://github.com/Kralizek/MinimalOpenApi/blob/master/docs/schema-feature-roadmap.md) and the installed package's release notes.
