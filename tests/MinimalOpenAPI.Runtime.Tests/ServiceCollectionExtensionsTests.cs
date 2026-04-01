using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using MinimalOpenAPI;

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

    [Test]
    public void MapOpenApiSchemas_WhenDirectoryDoesNotExist_ReturnsRouteGroupBuilder()
    {
        var app = WebApplication.CreateBuilder().Build();

        var group = app.MapOpenApiSchemas(schemasDirectory: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.That(group, Is.Not.Null);
    }

    [Test]
    public void MapOpenApiSchemas_WhenDirectoryIsEmpty_ReturnsRouteGroupBuilder()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var app = WebApplication.CreateBuilder().Build();

            var group = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(group, Is.Not.Null);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_WithYamlSchemaFile_WithVersion_RegistersEndpoint()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '2.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();

            var group = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(group, Is.Not.Null);
            // Verify an endpoint was registered in the group for the discovered schema file.
            // HTTP-level assertions (status code, content-type, body) are covered by
            // the integration test GetOpenApiSchema_ReturnsYamlWithCorrectContentType.
            Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Not.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_WithYamlSchemaFile_WithoutVersion_RegistersEndpoint()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"), "openapi: '3.0.0'");

            var app = WebApplication.CreateBuilder().Build();

            var group = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(group, Is.Not.Null);
            Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Not.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_WithJsonSchemaFile_RegistersEndpoint()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.json"),
                """{"openapi":"3.0.0","info":{"title":"My API","version":"1.0.0"}}""");

            var app = WebApplication.CreateBuilder().Build();

            var group = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(group, Is.Not.Null);
            Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Not.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_WithUnrecognisedExtension_SkipsFile()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            // A file with an unrecognised extension should be ignored.
            File.WriteAllText(Path.Combine(tempDir, "myapi.txt"), "not an openapi spec");

            var app = WebApplication.CreateBuilder().Build();

            var group = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(group, Is.Not.Null);
            Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}