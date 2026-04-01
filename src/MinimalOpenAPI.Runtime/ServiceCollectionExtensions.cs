using System.ComponentModel;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalOpenAPI;

/// <summary>
/// Extension methods for registering MinimalOpenAPI services and mapping endpoints.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static volatile Action<IServiceCollection>? _generatedRegistration;
    private static volatile Func<IEndpointRouteBuilder, string?, RouteGroupBuilder>? _generatedEndpointMapping;

    /// <summary>
    /// Called by the source-generated <c>[ModuleInitializer]</c> to wire up the
    /// generated handler and customizer registrations before the app starts.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterGeneratedServices(Action<IServiceCollection> registration)
    {
        _generatedRegistration = registration;
    }

    /// <summary>
    /// Called by the source-generated <c>[ModuleInitializer]</c> to wire up the
    /// generated endpoint mapping before the app starts.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterEndpointMapping(Func<IEndpointRouteBuilder, string?, RouteGroupBuilder> mapping)
    {
        _generatedEndpointMapping = mapping;
    }

    /// <summary>
    /// Adds the MinimalOpenAPI runtime services and all source-generated endpoint
    /// handlers to the DI container.
    /// </summary>
    public static IServiceCollection AddMinimalOpenApi(this IServiceCollection services)
    {
        _generatedRegistration?.Invoke(services);
        return services;
    }

    /// <summary>
    /// Maps all source-generated MinimalOpenAPI endpoints and returns a
    /// <see cref="RouteGroupBuilder"/> that can be further configured
    /// (e.g. <c>.RequireAuthorization()</c> to protect all endpoints at once).
    /// </summary>
    /// <param name="builder">The endpoint route builder.</param>
    /// <param name="prefix">Optional route prefix applied to all generated endpoints.</param>
    public static RouteGroupBuilder MapMinimalOpenApiEndpoints(
        this IEndpointRouteBuilder builder,
        string? prefix = null)
    {
        if (_generatedEndpointMapping is not null)
            return _generatedEndpointMapping(builder, prefix);

        // Fallback when no generator has run (e.g. missing OpenAPI spec).
        return builder.MapGroup(prefix ?? string.Empty);
    }

    /// <summary>
    /// Maps a GET endpoint for each <c>&lt;OpenApi Publish="true" /&gt;</c> spec file
    /// copied to the application base directory at build time, making each schema
    /// accessible at <c>{prefix}/{name}/schema.{ext}</c>
    /// (e.g. <c>/openapi/clients/schema.yaml</c>).
    /// </summary>
    /// <remarks>
    /// The method scans <paramref name="schemasDirectory"/> (or
    /// <c>AppContext.BaseDirectory/openapi</c> by default) for per-schema
    /// subdirectories created by the <c>CopyMinimalOpenApiFilesToOutput</c> MSBuild
    /// target, and registers one endpoint per discovered file.  This works for specs
    /// declared directly in the project as well as specs contributed via a NuGet
    /// contracts package (gRPC-style).
    /// </remarks>
    /// <param name="builder">The endpoint route builder.</param>
    /// <param name="prefix">
    /// Route prefix for all schema endpoints.  Defaults to <c>"/openapi"</c>.
    /// </param>
    /// <param name="schemasDirectory">
    /// Absolute path to the directory that contains the per-schema subdirectories.
    /// Defaults to <c>AppContext.BaseDirectory/openapi</c>.
    /// Override this parameter in tests to point at a temporary directory.
    /// </param>
    /// <returns>A <see cref="RouteGroupBuilder"/> that can be further configured.</returns>
    public static RouteGroupBuilder MapOpenApiSchemas(
        this IEndpointRouteBuilder builder,
        string? prefix = "/openapi",
        string? schemasDirectory = null)
    {
        var group = builder.MapGroup(prefix ?? "/openapi");
        var directory = schemasDirectory ?? Path.Combine(AppContext.BaseDirectory, "openapi");

        if (!Directory.Exists(directory))
        {
            return group;
        }

        foreach (var schemaDirectory in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(schemaDirectory);
            var schemaFile = Directory
                .EnumerateFiles(schemaDirectory, "schema.*")
                .FirstOrDefault();

            if (schemaFile is null)
            {
                continue;
            }

            var extension = Path.GetExtension(schemaFile).ToLowerInvariant();
            var contentType = extension switch
            {
                ".yaml" or ".yml" => "text/yaml",
                ".json" => "application/json",
                _ => "application/octet-stream",
            };

            group.MapGet(
                $"{name}/schema{extension}",
                // FileStreamHttpResult (returned by Results.File) disposes the stream
                // after the response is written, so the FileStream is correctly cleaned up.
                () => Results.File(File.OpenRead(schemaFile), contentType));
        }

        return group;
    }
}