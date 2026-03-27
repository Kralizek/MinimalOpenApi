using Microsoft.Extensions.DependencyInjection;

namespace MinimalOpenAPI;

/// <summary>
/// Extension methods for registering MinimalOpenAPI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the MinimalOpenAPI runtime services to the DI container.
    /// </summary>
    public static IServiceCollection AddMinimalOpenApi(this IServiceCollection services)
    {
        return services;
    }
}
