using MinimalOpenAPI;
using Microsoft.AspNetCore.Builder;

namespace MinimalOpenAPI.Runtime.Tests;

/// <summary>Tests for the AddMinimalOpenApi and MapMinimalOpenApiEndpoints extension methods.</summary>
[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [TearDown]
    public void TearDown()
    {
        // Reset static callbacks after each test so no state leaks to subsequent tests.
        ServiceCollectionExtensions.RegisterEndpointMapping(
            (builder, prefix) => builder.MapGroup(prefix ?? string.Empty));
    }

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

    [Test]
    public void RegisterEndpointMapping_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            ServiceCollectionExtensions.RegisterEndpointMapping(
                (builder, prefix) => builder.MapGroup(prefix ?? string.Empty)));
    }

    [Test]
    public void RegisterEndpointMapping_CalledMultipleTimes_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ServiceCollectionExtensions.RegisterEndpointMapping(
                (builder, prefix) => builder.MapGroup(prefix ?? string.Empty));
            ServiceCollectionExtensions.RegisterEndpointMapping(
                (builder, prefix) => builder.MapGroup(prefix ?? string.Empty));
        });
    }

    [Test]
    public void MapMinimalOpenApiEndpoints_WhenCallbackRegistered_InvokesCallback()
    {
        var app = WebApplication.CreateBuilder().Build();

        var invoked = false;
        ServiceCollectionExtensions.RegisterEndpointMapping((builder, prefix) =>
        {
            invoked = true;
            return builder.MapGroup(prefix ?? string.Empty);
        });

        var group = app.MapMinimalOpenApiEndpoints();

        Assert.That(invoked, Is.True);
        Assert.That(group, Is.Not.Null);
    }
}
