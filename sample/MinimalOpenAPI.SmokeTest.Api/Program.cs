using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

app.Run();
