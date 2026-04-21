using WithoutMinimalOpenApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTodoStore>();

var app = builder.Build();

app.MapTodoEndpoints();

app.Run();

public partial class Program;