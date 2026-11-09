# OpenAPI feature support and boundaries

Check the [repository feature matrix](https://github.com/Kralizek/MinimalOpenApi/blob/master/docs/schema-feature-roadmap.md) before choosing a construct. The table below summarizes the documented surface; it does **not** imply that an unsupported construct is invalid OpenAPI. These are limitations of **MinimalOpenAPI generation**.

## Unsupported or deferred constructs

| Construct | Guidance for consumers |
| --- | --- |
| `oneOf` | Not implemented. Do not assume discriminator-based polymorphic C# contracts will be generated. Redesign the contract with an explicitly modeled supported shape if the API semantics permit; otherwise discuss an alternative rather than silently changing semantics. |
| `anyOf` | Not implemented. Do not assume union semantics are generated; avoid replacing it with `allOf` (which means something different). |
| OpenAPI 2.0 / Swagger 2.0 | Not supported. Use an OpenAPI 3.0/3.1 contract. |
| Multipart form arrays of objects or dictionaries | Not supported; MOA011. Nested form objects and file arrays **are** supported. |
| Non-JSON text or binary/file responses | Generated response families are deferred; do not assume a `text/plain`, streaming, or file result is generated correctly. |
| Multiple response media types | Support is deferred; do not assume arbitrary content negotiation will be generated. |
| Other request media types | Deferred. JSON and multipart have explicit support; when both are declared JSON is preferred and selection is limited. |
| Parameter `style` / `explode` | Not generated; application-specific binding may be needed. |
| Response headers | Not generated as typed response contracts; set headers in application code using `HttpContext`. |
| DTO-property `default` | Not generated; operation-parameter scalar defaults are supported. |
| `example` / `examples` | Not generated into C#; keep them in the authored document for documentation tools. |
| `deprecated` | No generated `[Obsolete]` equivalent yet. |

## Partial support and runtime caveats

- Cookie parameters appear in generated parameter models, but ASP.NET Core doesn't automatically bind them; access `HttpContext` or supply application binding.
- Generated `DataAnnotations` attributes do **not** execute validation automatically in ASP.NET Core Minimal APIs; configure validation.
- `allOf` object flattening is supported, but incompatible properties can trigger MOA007 with a `JsonElement` fallback. This is not general union support.
- Security declarations do not configure authentication or authorization for you; apply ASP.NET Core policies using endpoint configuration.

An agent must not quietly discard unsupported OpenAPI semantics, substitute an unrelated construct, or claim support based solely on OpenAPI validity. Explain the constraint, inspect the generator diagnostics, and agree on a contract-preserving path where possible.

For precise details and future changes, consult [the feature support matrix](https://github.com/Kralizek/MinimalOpenApi/blob/master/docs/schema-feature-roadmap.md) and the installed package's release notes.
