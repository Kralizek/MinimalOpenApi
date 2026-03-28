using MinimalOpenAPI;

namespace MinimalOpenAPI.Runtime.Tests;

/// <summary>Tests for the AddMinimalOpenApi extension method.</summary>
[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddMinimalOpenApi_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddMinimalOpenApi();

        Assert.That(result, Is.SameAs(services));
    }

    [Test]
    public void AddMinimalOpenApi_CanBuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMinimalOpenApi();

        Assert.DoesNotThrow(() => services.BuildServiceProvider());
    }

    [Test]
    public void AddMinimalOpenApi_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Assert.DoesNotThrow(() =>
        {
            services.AddMinimalOpenApi();
            services.AddMinimalOpenApi();
        });
    }
}
