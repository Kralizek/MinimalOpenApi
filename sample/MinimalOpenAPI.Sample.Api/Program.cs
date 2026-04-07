using MinimalOpenAPI;
using MinimalOpenAPI.Sample.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTodoStore>();
builder.Services.AddMinimalOpenApi();

var app = builder.Build();

// Note: if you pass a prefix to MapMinimalOpenApiEndpoints (e.g. "/api"), the paths
// in the published spec file will not reflect that prefix. In that case Swagger UI's
// "Try it out" will use the paths from the spec, which may not match the actual routes.
// For Swagger UI integration, use the default (no prefix) or update the spec accordingly.
app.MapMinimalOpenApiEndpoints();

// Map every <OpenApi Publish="true" /> spec file as a static GET endpoint and get back
// descriptors for each one. The descriptor carries the public HTTP path, display name,
// and the RouteHandlerBuilder so you can further configure individual schema endpoints.
var schemas = app.MapOpenApiSchemas();

// Configure Swagger UI to display every published spec as a named endpoint.
// The public path and display name come directly from the schema map result,
// so there is a single source of truth for the schema endpoint URLs.
// Swagger UI will be available at /swagger/index.html.
app.UseSwaggerUI(options =>
{
    foreach (var schema in schemas.Schemas)
    {
        options.SwaggerEndpoint(schema.PublicPath, schema.Name);
    }
});

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }