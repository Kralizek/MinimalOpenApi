using Microsoft.Extensions.FileProviders;

using MinimalOpenAPI;
using MinimalOpenAPI.Sample.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTodoStore>();
builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

// Serve published OpenAPI spec files from the openapi/ output subdirectory.
// The MinimalOpenAPI build targets copy <OpenApi Publish="true" /> files there
// during build and publish, preserving the original content at a deterministic
// path: openapi/<name>/schema.<extension>  (e.g. openapi/openapi/schema.yaml).
// With this configuration the spec is accessible at /openapi/<name>/schema.<ext>.
var openApiDirectory = Path.Combine(AppContext.BaseDirectory, "openapi");
if (Directory.Exists(openApiDirectory))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(openApiDirectory),
        RequestPath = "/openapi",
    });
}

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }