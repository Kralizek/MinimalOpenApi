# Configuring generated MinimalOpenAPI endpoints

Use the generated `<OperationId>EndpointConfigurationBase` hook for **endpoint-specific ASP.NET Core routing conventions and policies**. Keep handler business logic in `<OperationId>EndpointBase`, observable responses in the authored OpenAPI contract, and application-wide middleware/services in the application's startup.

## Per-operation configuration

For an OpenAPI operation with `operationId: getOrder`, the generator creates an abstract `GetOrderEndpointConfigurationBase`. The application may optionally implement one concrete subclass:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

public sealed class GetOrderEndpointConfiguration
    : GetOrderEndpointConfigurationBase
{
    public override void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireAuthorization("Orders.Read")
            .RequireRateLimiting("api");
    }
}
```

Build first to inspect the generated base class and namespace. The generator discovers the concrete subclass, registers it, and invokes `Configure` for the matching operation during `app.MapMinimalOpenApiEndpoints()`. **Do not instantiate, resolve, or register it manually.** No configuration implementation is required if the operation needs no special conventions; more than one concrete implementation produces `MOA003`.

The generator applies contract-derived metadata (such as operation name, summary, tags, and declared responses) before calling `Configure`. The hook runs at **endpoint mapping time**, not on every HTTP request. The configuration is registered as a **singleton**; avoid request-scoped dependencies, request state, or work that belongs in handlers/middleware. Use endpoint filters or middleware when behavior must execute per request.

## Register the supporting application services

`RequireAuthorization("Orders.Read")` and `RequireRateLimiting("api")` attach policies to the endpoint; they do not create those policies or set up the required authentication, authorization, and rate-limiting services/middleware. Configure those using normal ASP.NET Core APIs, for example:

```csharp
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddAuthorization(options =>
    options.AddPolicy("Orders.Read", policy => policy.RequireAuthenticatedUser()));
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
    }));
// Configure the application's real authentication scheme separately.

builder.Services.AddMinimalOpenApi();

var app = builder.Build();
app.UseRouting();
app.UseRateLimiter(); // After UseRouting for endpoint-specific policies.
// Configure UseAuthentication / UseAuthorization for the real auth scheme as appropriate.
app.MapMinimalOpenApiEndpoints();
app.Run();
```

This is a **policy-wiring illustration**, not a complete authentication example. Confirm any ordering requirements for middleware (including routing and authorization) in the target application and integration-test the actual 401/403/429 behavior. Filtering 401/403 out of handler result types does not configure those responses; see [response handling](response-handling.md).

Other uses of `Configure` include `AddEndpointFilter(...)`, `WithMetadata(...)`, `WithTags(...)`, `DisableAntiforgery()` where justified, and request-size or other endpoint conventions. Consider security and validation implications before disabling any protection. Applying configuration after generated metadata does **not** guarantee every convention overrides earlier metadata; ASP.NET Core decides how metadata is interpreted.

## Policies shared by all generated endpoints

`MapMinimalOpenApiEndpoints()` returns a `RouteGroupBuilder`, so conventions can also be applied to the **whole generated endpoint group**:

```csharp
app.MapMinimalOpenApiEndpoints()
   .RequireAuthorization();
```

This affects endpoints from **all OpenAPI documents registered in that application**, because MOA maps them onto a single shared route group. Do not use the group to apply a policy intended for only one operation or one document. Use per-operation configuration for narrower policy boundaries.

The generated route group's behavior is distinct from `MapOpenApiSchemas()`. Schema-serving endpoints are mapped separately and only exist when `PublishAs` is configured. They are not automatically protected by policies attached to `MapMinimalOpenApiEndpoints()`. If an authored contract should not be publicly accessible, either do not publish it or explicitly secure its mapped schema endpoint using the descriptors returned by `MapOpenApiSchemas()`.

## Troubleshooting and validation

- **Policy not enforced:** Check that the concrete class inherits from the generated configuration base, is discovered by the generator, and that the policy/services/middleware are actually configured. Check the endpoint at runtime, not just its metadata.
- **MOA003:** Remove duplicate concrete configuration implementations for the same operation.
- **Scoped service injection fails or leaks state:** Configuration objects are singletons and run during mapping. Move request-time work into a filter, middleware, or handler.
- **Unexpected global authorization:** Inspect conventions added to the `RouteGroupBuilder`, especially in applications with multiple OpenAPI documents.
- **Published schema unexpectedly accessible:** Schema endpoints are separate mappings; review `PublishAs` and explicit schema endpoint policies.

After changing configuration, build and run HTTP-level tests covering allowed/denied requests, rate limits or filters, and any relevant error responses. Compare with existing endpoint behavior during migration.

Upstream reference: [getting started](https://github.com/Kralizek/MinimalOpenApi/blob/master/docs/getting-started.md).
