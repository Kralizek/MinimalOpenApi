# Response ownership and handler result unions (1.1+)

The OpenAPI contract describes **every observable response**, even a status produced by authentication/authorization middleware or an infrastructure exception handler. The generated handler's result type should list responses **the handler actually produces**. Never delete contract response entries simply to avoid implementing them.

For example, where an operation declares 200/401/403/404 and authentication infrastructure (actually configured by the application) produces 401/403:

```xml
<ItemGroup>
  <OpenApi Include="openapi.yaml" ExcludeStatusCodesFromHandlers="401;403" />
</ItemGroup>
```

The declared 401 and 403 remain in OpenAPI and generated endpoint metadata. Filtering changes only the handler signature. A global exclusion applies to every operation in that document; use per-operation items when ownership differs:

```xml
<MinimalOpenApiHandler Include="getOrder"
                       IncludeStatusCodes="403"
                       ExcludeStatusCodes="409" />
```

Here `Include` names the **operationId**, not the file or a type name. `IncludeStatusCodes` re-includes a globally excluded status when the handler owns it. `ExcludeStatusCodes` excludes a handler-unowned status; exclusions win if both settings mention a status. Filtering precedence: global excludes, per-operation includes, per-operation excludes. Every specified status must be declared by that operation; invalid selectors or codes raise MOA015.

Multiple operation IDs can be configured with normal MSBuild semicolon expansion:

```xml
<MinimalOpenApiHandler Include="createName;updateName"
                       ExcludeStatusCodes="400" />
```

For multiple documents, set `OpenApiFile` to the **full configured item path** (e.g. `contracts/orders/openapi.yaml`). An explicit selector is validated even when there is just one document.

**Do not assume 400, 401, 403 or 500 belong to infrastructure automatically.** Determine the actual producer for each endpoint. A missing authentication middleware setup does not magically implement 401/403. Do not filter business-owned responses just to reduce generic arity.

Distinct handler response types are emitted in OpenAPI declaration order (deduplicated by result type). Two through six use flat ASP.NET Core `Results<...>`; more than six use **recursive tail-nested unions** (first five alternatives plus a sixth nested `Results<...>`). A nested alternative may need explicit conversion to its inner union and then outer union. Always inspect the generated `HandleAsync` signature after response changes, and adapt the override.

See [the ResponseResults sample](../../../sample/ResponseResults/README.md) for a tested example.
