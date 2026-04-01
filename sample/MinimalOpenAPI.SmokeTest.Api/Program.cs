using Microsoft.Extensions.FileProviders;

using MinimalOpenAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalOpenApi();

var app = builder.Build();

app.MapMinimalOpenApiEndpoints();

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
