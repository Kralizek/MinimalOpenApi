using MinimalOpenAPI;

using WithMinimalOpenApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTodoStore>();
builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

app.Run();

public partial class Program;