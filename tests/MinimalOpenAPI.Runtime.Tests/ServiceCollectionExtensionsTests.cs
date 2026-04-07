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
        // Reset static state after each test so no state leaks to subsequent tests.
        ServiceCollectionExtensions.ResetForTesting();
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
                (builder, group) => { }));
    }

    [Test]
    public void RegisterEndpointMapping_CalledMultipleTimes_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ServiceCollectionExtensions.RegisterEndpointMapping(
                (builder, group) => { });
            ServiceCollectionExtensions.RegisterEndpointMapping(
                (builder, group) => { });
        });
    }

    [Test]
    public void MapMinimalOpenApiEndpoints_WhenCallbackRegistered_InvokesCallback()
    {
        var app = WebApplication.CreateBuilder().Build();

        var invoked = false;
        ServiceCollectionExtensions.RegisterEndpointMapping((builder, group) =>
        {
            invoked = true;
        });

        var group = app.MapMinimalOpenApiEndpoints();

        Assert.That(invoked, Is.True);
        Assert.That(group, Is.Not.Null);
    }

    [Test]
    public void MapMinimalOpenApiEndpoints_MultipleCallbacks_AllAreInvoked()
    {
        var app = WebApplication.CreateBuilder().Build();

        var invoked1 = false;
        var invoked2 = false;
        ServiceCollectionExtensions.RegisterEndpointMapping((builder, group) => { invoked1 = true; });
        ServiceCollectionExtensions.RegisterEndpointMapping((builder, group) => { invoked2 = true; });

        var group = app.MapMinimalOpenApiEndpoints();

        Assert.That(invoked1, Is.True);
        Assert.That(invoked2, Is.True);
        Assert.That(group, Is.Not.Null);
    }

    [Test]
    public void MapMinimalOpenApiEndpoints_NoCallbackRegistered_ReturnsRouteGroupBuilder()
    {
        var app = WebApplication.CreateBuilder().Build();

        var group = app.MapMinimalOpenApiEndpoints();

        Assert.That(group, Is.Not.Null);
    }

    [Test]
    public void AddMinimalOpenApi_MultipleRegistrations_AllAreInvoked()
    {
        var services = new ServiceCollection();

        var count = 0;
        ServiceCollectionExtensions.RegisterGeneratedServices(_ => count++);
        ServiceCollectionExtensions.RegisterGeneratedServices(_ => count++);

        services.AddMinimalOpenApi();

        Assert.That(count, Is.EqualTo(2));
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

    [Test]
    public void RegisterSchemaFile_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/123456789/openapi.yaml"));
    }

    [Test]
    public void RegisterSchemaFile_CalledMultipleTimes_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/111111111/api-v1.yaml");
            ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/222222222/api-v2.yaml");
        });
    }

    [Test]
    public void MapOpenApiSchemas_WithRegisteredSchemaFiles_UsesRegisteredFilesInsteadOfDirectoryScan()
    {
        // Register the relative path (as the generated module initializer would).
        ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/987654321/myapi.yaml");

        // The flat scan directory is empty — if MapOpenApiSchemas fell back to scanning it, no
        // endpoints would be registered.  With a registered file the endpoint must still appear,
        // confirming that the registered-file path takes precedence over directory scanning.
        var app = WebApplication.CreateBuilder().Build();
        var emptyDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var group = app.MapOpenApiSchemas(schemasDirectory: emptyDir);

            Assert.That(group, Is.Not.Null);
            Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Not.Empty,
                "An endpoint should be registered from the registered schema file, not from the empty schemasDirectory.");
        }
        finally
        {
            Directory.Delete(emptyDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_WithRegisteredSchemaFiles_IgnoresDirectoryScan()
    {
        // When files ARE registered, a non-empty schemasDirectory must not contribute extra endpoints.
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            // Register one schema file (path does not need to exist for endpoint-count assertions).
            ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/111111111/registered.yaml");

            // Put an extra file in the scan directory — it should be ignored because registered
            // files take precedence, so only 1 DataSource (from the registered file) is added.
            File.WriteAllText(Path.Combine(tempDir, "extra.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: Extra\n  version: '9.9.9'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var group = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            // DataSources count reflects registered files only (1), not the extra scan file.
            Assert.That(((IEndpointRouteBuilder)group).DataSources, Has.Count.EqualTo(1));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void RegisterSchemaFile_WithPublishPathOverride_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            ServiceCollectionExtensions.RegisterSchemaFile(
                "openapi/schemas/123456789/openapi.yaml",
                "/contracts/public/v1/openapi.yaml"));
    }

    [Test]
    public void MapOpenApiSchemas_WithPublishPathOverride_RegistersEndpointOnBuilder()
    {
        // A registered file with a PublishPathOverride should be mapped directly on the
        // root builder (bypassing the group prefix) so the endpoint is accessible at the
        // exact override path.
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/myapi.yaml",
            "/contracts/public/v1/openapi.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var group = app.MapOpenApiSchemas();

        Assert.That(group, Is.Not.Null);
        // The override endpoint is registered on the root app, not on the prefixed group.
        // The group itself should have no data sources for this file.
        Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Empty,
            "Override-path endpoint must be registered on the root builder, not inside the prefixed group.");
        // The root app should have endpoints registered (one group from MapGroup + override route).
        Assert.That(((IEndpointRouteBuilder)app).DataSources, Is.Not.Empty);
    }

    [Test]
    public void MapOpenApiSchemas_MixedRegistrations_RegistersCorrectEndpoints()
    {
        // File without override goes into the prefixed group; file with override goes to root.
        ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/111111111/api.yaml");
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/222222222/billing.yaml",
            "/swagger/billing/v2/schema.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var group = app.MapOpenApiSchemas();

        Assert.That(group, Is.Not.Null);
        // The default-routed file contributes a data source to the prefixed group.
        Assert.That(((IEndpointRouteBuilder)group).DataSources, Is.Not.Empty,
            "Default-routed schema should be registered in the prefixed group.");
    }

    [Test]
    public void MapOpenApiSchemas_WithDuplicatePublishPathOverride_Throws()
    {
        // Two files with the same PublishPathOverride should cause MapOpenApiSchemas to throw.
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/111111111/api-v1.yaml",
            "/contracts/v1/schema.yaml");
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/222222222/api-v2.yaml",
            "/contracts/v1/schema.yaml");

        var app = WebApplication.CreateBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => app.MapOpenApiSchemas(),
            "Duplicate PublishPathOverride values must cause MapOpenApiSchemas to throw.");
    }

    [Test]
    public void RegisterSchemaFile_WithPublishPathOverride_CalledMultipleTimes_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ServiceCollectionExtensions.RegisterSchemaFile(
                "openapi/schemas/111111111/api-v1.yaml",
                "/contracts/v1/schema.yaml");
            ServiceCollectionExtensions.RegisterSchemaFile(
                "openapi/schemas/222222222/api-v2.yaml",
                "/contracts/v2/schema.yaml");
        });
    }
}