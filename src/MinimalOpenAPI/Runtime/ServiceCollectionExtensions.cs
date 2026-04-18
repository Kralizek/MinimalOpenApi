#if NET10_0_OR_GREATER
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
    private static readonly List<Action<IServiceCollection>> _generatedRegistrations = new();
    private static readonly List<Action<IEndpointRouteBuilder, RouteGroupBuilder>> _generatedEndpointMappings = new();
    private static readonly List<(string RelPath, string? PublishAs, string? DisplayName, string? DisplayVersion)> _registeredSchemaFiles = new();
    private static readonly object _registrationLock = new();

    /// <summary>
    /// Called by the source-generated <c>[ModuleInitializer]</c> to wire up the
    /// generated handler and customizer registrations before the app starts.
    /// Each OpenAPI spec file registered in the project contributes one registration
    /// callback; multiple specs are supported by calling this method once per spec.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterGeneratedServices(Action<IServiceCollection> registration)
    {
        lock (_registrationLock)
            _generatedRegistrations.Add(registration);
    }

    /// <summary>
    /// Called by the source-generated <c>[ModuleInitializer]</c> to wire up the
    /// generated endpoint mapping before the app starts.
    /// Each OpenAPI spec file registered in the project contributes one mapping
    /// callback; multiple specs are supported by calling this method once per spec.
    /// The callback receives the service provider and the pre-created
    /// <see cref="RouteGroupBuilder"/> and maps endpoints directly onto it.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterEndpointMapping(Action<IEndpointRouteBuilder, RouteGroupBuilder> mapping)
    {
        lock (_registrationLock)
            _generatedEndpointMappings.Add(mapping);
    }

    /// <summary>
    /// Called by the source-generated <c>[ModuleInitializer]</c> to register the
    /// path (relative to <see cref="AppContext.BaseDirectory"/>) at which the
    /// corresponding OpenAPI spec file was copied at build time.
    /// <see cref="MapOpenApiSchemas"/> uses these registrations to serve each spec
    /// file at the correct endpoint URL without scanning the output directory.
    /// </summary>
    /// <param name="relPath">
    /// Path relative to <see cref="AppContext.BaseDirectory"/>, using forward slashes,
    /// e.g. <c>openapi/schemas/987654321/openapi.yaml</c>.
    /// </param>
    /// <param name="publishAs">The optional HTTP route path at which this schema file is exposed.</param>
    /// <param name="displayName">The optional display name for schema descriptors returned by <see cref="MapOpenApiSchemas"/>.</param>
    /// <param name="displayVersion">The optional display version for schema descriptors returned by <see cref="MapOpenApiSchemas"/>.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterSchemaFile(
        string relPath,
        string? publishAs = null,
        string? displayName = null,
        string? displayVersion = null)
    {
        lock (_registrationLock)
            _registeredSchemaFiles.Add((relPath, publishAs, displayName, displayVersion));
    }

    /// <summary>
    /// Adds the MinimalOpenAPI runtime services and all source-generated endpoint
    /// handlers to the DI container.
    /// </summary>
    public static IServiceCollection AddMinimalOpenApi(this IServiceCollection services)
    {
        List<Action<IServiceCollection>> registrations;
        lock (_registrationLock)
            registrations = new List<Action<IServiceCollection>>(_generatedRegistrations);

        foreach (var reg in registrations)
            reg(services);

        return services;
    }

    /// <summary>
    /// Maps all source-generated MinimalOpenAPI endpoints and returns a
    /// <see cref="RouteGroupBuilder"/> that can be further configured
    /// (e.g. <c>.RequireAuthorization()</c> to protect all endpoints at once).
    /// When multiple OpenAPI spec files are registered, all of their endpoints are
    /// mapped onto the same group.
    /// </summary>
    /// <param name="builder">The endpoint route builder.</param>
    public static RouteGroupBuilder MapMinimalOpenApiEndpoints(
        this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup(string.Empty);

        List<Action<IEndpointRouteBuilder, RouteGroupBuilder>> mappings;
        lock (_registrationLock)
            mappings = new List<Action<IEndpointRouteBuilder, RouteGroupBuilder>>(_generatedEndpointMappings);

        foreach (var mapping in mappings)
            mapping(builder, group);

        return group;
    }

    /// <summary>
    /// Clears all registered service and endpoint-mapping callbacks.
    /// This method is intended for use in unit tests only to prevent state leaking between tests.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void ResetForTesting()
    {
        lock (_registrationLock)
        {
            _generatedRegistrations.Clear();
            _generatedEndpointMappings.Clear();
            _registeredSchemaFiles.Clear();
        }
    }

    /// <summary>
    /// Maps endpoints that serve registered OpenAPI schema files as static HTTP responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the source generator has emitted <c>RegisterSchemaFile</c> calls (the normal case
    /// for projects that reference the <c>MinimalOpenAPI</c> package), only entries that have
    /// <c>PublishAs</c> are mapped. The registered paths point to
    /// the unique per-file subdirectories created by the <c>CopyMinimalOpenApiFilesToOutput</c>
    /// MSBuild target, so two spec files with the same filename in different source directories
    /// or NuGet packages never collide.
    /// </para>
    /// </remarks>
    /// <param name="builder">The endpoint route builder.</param>
    /// <param name="prefix">
    /// Legacy parameter retained for source compatibility. Not used by explicit schema mapping.
    /// </param>
    /// <param name="schemasDirectory">
    /// Legacy parameter retained for source compatibility. Not used by explicit schema mapping.
    /// </param>
    /// <returns>
    /// An <see cref="OpenApiSchemaMapResult"/> describing every mapped schema endpoint,
    /// including the public HTTP path and the <see cref="RouteHandlerBuilder"/> for further
    /// configuration.  The return value may be ignored when the endpoint metadata is not needed.
    /// </returns>
    public static OpenApiSchemaMapResult MapOpenApiSchemas(
        this IEndpointRouteBuilder builder,
        string? prefix = "/.openapi",
        string? schemasDirectory = null)
    {
        _ = prefix;
        _ = schemasDirectory;
        var descriptors = new List<OpenApiSchemaEndpoint>();

        List<(string RelPath, string? PublishAs, string? DisplayName, string? DisplayVersion)> registeredFiles;
        lock (_registrationLock)
            registeredFiles = new List<(string, string?, string?, string?)>(_registeredSchemaFiles);

        var publishAsPaths = registeredFiles
            .Where(f => !string.IsNullOrWhiteSpace(f.PublishAs))
            .Select(f => f.PublishAs!)
            .ToList();
        var duplicates = publishAsPaths
            .GroupBy(p => p, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate PublishAs values detected: {string.Join(", ", duplicates)}. " +
                "Each published schema must have a unique HTTP path.");
        }

        foreach (var (relPath, publishAs, displayName, displayVersion) in registeredFiles)
        {
            if (string.IsNullOrWhiteSpace(publishAs))
                continue;

            var absolutePath = Path.Combine(
                AppContext.BaseDirectory,
                relPath.Replace('/', Path.DirectorySeparatorChar));

            var endpoint = MapSchemaFileEndpointAtPath(builder, absolutePath, publishAs);
            var fallbackName = Path.GetFileNameWithoutExtension(relPath);
            var name = string.IsNullOrWhiteSpace(displayName) ? fallbackName : displayName;
            var version = string.IsNullOrWhiteSpace(displayVersion) ? null : displayVersion;
            descriptors.Add(new OpenApiSchemaEndpoint(name, version, publishAs, endpoint));
        }

        return new OpenApiSchemaMapResult(descriptors);
    }

    /// <summary>
    /// Registers a single GET endpoint directly on <paramref name="builder"/> that serves
    /// <paramref name="absolutePath"/> at the verbatim <paramref name="overridePath"/>.
    /// The content type is derived from the source file extension.
    /// </summary>
    private static RouteHandlerBuilder MapSchemaFileEndpointAtPath(IEndpointRouteBuilder builder, string absolutePath, string overridePath)
    {
        var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".yaml" or ".yml" => "text/yaml",
            ".json" => "application/json",
            _ => "application/octet-stream",
        };

        return builder.MapGet(
            overridePath,
            () => Results.File(File.OpenRead(absolutePath), contentType));
    }
}
#endif