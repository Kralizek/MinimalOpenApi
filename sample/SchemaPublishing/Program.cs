using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

// Map every <OpenApi PublishAs="..." /> spec file as a static GET endpoint and get back
// descriptors for each one. The descriptor carries the public HTTP path, display name,
// version metadata, and the RouteHandlerBuilder so you can further configure schema endpoints.
//
// Only public-api.yaml has PublishAs, so only that file is mapped as an HTTP endpoint.
// internal-api.yaml is still copied to build/publish output but is not served over HTTP.
var schemas = app.MapOpenApiSchemas();

// Configure Swagger UI to display every published spec as a named endpoint.
// The public path and display name come directly from the schema map result,
// so there is a single source of truth for the schema endpoint URLs.
// Swagger UI will be available at /swagger/index.html.
app.UseSwaggerUI(options =>
{
    foreach (var schema in schemas.Schemas)
    {
        options.SwaggerEndpoint(schema.PublicPath, $"{schema.Name} {schema.Version}");
    }
});

app.Run();