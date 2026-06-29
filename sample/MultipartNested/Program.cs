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

        builder.Services.AddAntiforgery();
        builder.Services.AddMinimalOpenApi();

        var app = builder.Build();

        // This is a REST API consumed by non-browser clients; CSRF protection is not required.
        // DisableAntiforgery() removes the antiforgery-required metadata that [FromForm]
        // endpoints emit automatically, so no antiforgery middleware is needed.
        app.UseAntiforgery();
        app.MapMinimalOpenApiEndpoints().DisableAntiforgery();

        await app.RunAsync();
    }
}
