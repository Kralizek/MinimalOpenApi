using MinimalOpenAPI;

namespace MinimalOpenAPI.Samples.ClassLibrary.Host;

/// <summary>
/// Entry point. It deliberately does not touch any type from the Catalog or Billing libraries:
/// their endpoints must be registered by MinimalOpenAPI alone.
/// Declared as a named class so integration tests can reference several sample hosts.
/// </summary>
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMinimalOpenApi();

        var app = builder.Build();

        app.MapMinimalOpenApiEndpoints();

        await app.RunAsync();
    }
}