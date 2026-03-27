using MinimalOpenAPI;

namespace MinimalOpenAPI.Runtime.Tests;

/// <summary>Tests for the AddMinimalOpenApi extension method.</summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMinimalOpenApi_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddMinimalOpenApi();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddMinimalOpenApi_CanBuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMinimalOpenApi();

        var action = () => services.BuildServiceProvider();

        action.Should().NotThrow();
    }

    [Fact]
    public void AddMinimalOpenApi_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var action = () =>
        {
            services.AddMinimalOpenApi();
            services.AddMinimalOpenApi();
        };

        action.Should().NotThrow();
    }
}
