using Microsoft.Extensions.DependencyInjection;

namespace MinimalOpenAPI.Runtime;

/// <summary>
/// Extension methods for registering MinimalOpenAPI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static Action<IServiceCollection>? _generatedRegistration;

    /// <summary>
    /// Called by the source-generated <c>[ModuleInitializer]</c> to wire up the
    /// generated handler and customizer registrations before the app starts.
    /// </summary>
    public static void RegisterGeneratedServices(Action<IServiceCollection> registration)
    {
        _generatedRegistration = registration;
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
}
