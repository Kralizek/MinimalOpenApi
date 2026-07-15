using MinimalOpenAPI;

namespace MinimalOpenAPI.Samples.MultipartNested;

/// <summary>
/// Entry point for the MultipartNested sample. Declared as a named class (rather than using
/// top-level statements) so that the integration-test project can reference both this sample
/// and BasicTodo without hitting a duplicate global <c>Program</c> class.
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