using MinimalOpenAPI;
using MinimalOpenAPI.Sample.Api;
using MinimalOpenAPI.Sample.Api.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTodoStore>();
builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
