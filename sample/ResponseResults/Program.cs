using MinimalOpenAPI;
using MinimalOpenAPI.Samples.ResponseResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryOrderStore>();
builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

app.Run();