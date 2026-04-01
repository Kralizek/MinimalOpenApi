using MinimalOpenAPI;
using MinimalOpenAPI.Sample.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTodoStore>();
builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

// Serve every <OpenApi Publish="true" /> spec at GET /openapi/{name}/schema.{ext}.
// The MinimalOpenAPI build targets copy those files to the application base directory
// at build and publish time; MapOpenApiSchemas() discovers them and registers endpoints.
app.MapOpenApiSchemas();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }