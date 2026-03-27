using MinimalOpenAPI;
using MinimalOpenAPI.Sample.Api;
using MinimalOpenAPI.Sample.Api.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalOpenApi();
builder.Services.AddSingleton<InMemoryClientStore>();
builder.Services.AddGeneratedEndpoints();

var app = builder.Build();

app.MapEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
