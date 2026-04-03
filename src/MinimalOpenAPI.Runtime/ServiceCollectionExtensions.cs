using System.ComponentModel;
using System.Text.RegularExpressions;

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
    private static readonly List<string> _registeredSchemaFiles = new();
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
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterSchemaFile(string relPath)
    {
        lock (_registrationLock)
            _registeredSchemaFiles.Add(relPath);
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
    /// mapped onto the same group under the shared <paramref name="prefix"/>.
    /// </summary>
    /// <param name="builder">The endpoint route builder.</param>
    /// <param name="prefix">Optional route prefix applied to all generated endpoints.</param>
    public static RouteGroupBuilder MapMinimalOpenApiEndpoints(
        this IEndpointRouteBuilder builder,
        string? prefix = null)
    {
        var group = builder.MapGroup(prefix ?? string.Empty);

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
    /// Maps endpoints that serve each <c>&lt;OpenApi Publish="true" /&gt;</c> spec file as a
    /// static HTTP response, making each schema accessible at
    /// <c>{prefix}/schemas/{version}/{name}.{ext}</c>
    /// (e.g. <c>/.openapi/schemas/1.0.0/clients.yaml</c>).
    /// When the <c>info.version</c> field cannot be determined the version segment is
    /// omitted and the endpoint is registered at <c>{prefix}/schemas/{name}.{ext}</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the source generator has emitted <c>RegisterSchemaFile</c> calls (the normal case
    /// for projects that reference the <c>MinimalOpenAPI</c> package), those registered paths
    /// are used directly and no filesystem scan is performed.  The registered paths point to
    /// the unique per-file subdirectories created by the <c>CopyMinimalOpenApiFilesToOutput</c>
    /// MSBuild target, so two spec files with the same filename in different source directories
    /// or NuGet packages never collide.
    /// </para>
    /// <para>
    /// When no files have been registered (e.g. in tests that call <c>MapOpenApiSchemas</c>
    /// directly without going through the generated module initializer), the method falls back
    /// to scanning <paramref name="schemasDirectory"/> for spec files.
    /// </para>
    /// </remarks>
    /// <param name="builder">The endpoint route builder.</param>
    /// <param name="prefix">
    /// Route prefix for all schema endpoints.  Defaults to <c>"/.openapi"</c>.
    /// </param>
    /// <param name="schemasDirectory">
    /// Absolute path to the directory that contains the spec files used by the fallback scan.
    /// Defaults to <c>AppContext.BaseDirectory/openapi/schemas</c>.
    /// Override this parameter in tests to point at a temporary directory.
    /// </param>
    /// <returns>A <see cref="RouteGroupBuilder"/> that can be further configured.</returns>
    public static RouteGroupBuilder MapOpenApiSchemas(
        this IEndpointRouteBuilder builder,
        string? prefix = "/.openapi",
        string? schemasDirectory = null)
    {
        var group = builder.MapGroup(prefix ?? "/.openapi");

        List<string> registeredFiles;
        lock (_registrationLock)
            registeredFiles = new List<string>(_registeredSchemaFiles);

        if (registeredFiles.Count > 0)
        {
            // Use files registered by the source-generated module initializer.
            // Each path is relative to AppContext.BaseDirectory and uses forward slashes.
            foreach (var relPath in registeredFiles)
            {
                var absolutePath = Path.Combine(
                    AppContext.BaseDirectory,
                    relPath.Replace('/', Path.DirectorySeparatorChar));
                MapSchemaFileEndpoint(group, absolutePath);
            }
        }
        else
        {
            // Fallback: scan the schemas directory (used when no files were registered via
            // RegisterSchemaFile, e.g. in tests that exercise MapOpenApiSchemas directly).
            var directory = schemasDirectory ?? Path.Combine(AppContext.BaseDirectory, "openapi", "schemas");

            if (!Directory.Exists(directory))
                return group;

            foreach (var schemaFile in Directory.EnumerateFiles(directory))
                MapSchemaFileEndpoint(group, schemaFile);
        }

        return group;
    }

    /// <summary>
    /// Registers a single GET endpoint on <paramref name="group"/> that serves
    /// <paramref name="absolutePath"/> at <c>schemas/{version}/{name}{ext}</c>
    /// (or <c>schemas/{name}{ext}</c> when no version is found).
    /// Files whose extension is not <c>.yaml</c>, <c>.yml</c>, or <c>.json</c> are skipped.
    /// </summary>
    private static void MapSchemaFileEndpoint(RouteGroupBuilder group, string absolutePath)
    {
        var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
        if (extension is not (".yaml" or ".yml" or ".json"))
            return;

        var name = Path.GetFileNameWithoutExtension(absolutePath);
        var contentType = extension switch
        {
            ".yaml" or ".yml" => "text/yaml",
            ".json" => "application/json",
            _ => "application/octet-stream",
        };

        var version = ExtractVersion(absolutePath);
        var routePath = version is not null
            ? $"schemas/{version}/{name}{extension}"
            : $"schemas/{name}{extension}";

        group.MapGet(
            routePath,
            // FileStreamHttpResult (returned by Results.File) disposes the stream
            // after the response is written, so the FileStream is correctly cleaned up.
            () => Results.File(File.OpenRead(absolutePath), contentType));
    }

    /// <summary>
    /// Extracts the <c>info.version</c> value from an OpenAPI spec file (YAML or JSON)
    /// using lightweight pattern matching, without a full parser dependency.
    /// Returns <see langword="null"/> when the version cannot be determined.
    /// </summary>
    private static string? ExtractVersion(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);

            // YAML: version: "1.0.0"  or  version: 1.0.0  or  version: '1.0.0'
            var yamlMatch = Regex.Match(
                content,
                @"^\s*version:\s*['""]?([^\s'""\n\r]+)['""]?\s*$",
                RegexOptions.Multiline);
            if (yamlMatch.Success)
                return yamlMatch.Groups[1].Value;

            // JSON: "version": "1.0.0"
            var jsonMatch = Regex.Match(content, @"""version""\s*:\s*""([^""]+)""");
            if (jsonMatch.Success)
                return jsonMatch.Groups[1].Value;

            return null;
        }
        catch
        {
            return null;
        }
    }
}