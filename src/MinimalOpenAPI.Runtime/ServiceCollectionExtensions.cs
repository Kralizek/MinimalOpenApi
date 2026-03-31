using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
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
}