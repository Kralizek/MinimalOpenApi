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
    public void MapOpenApiSchemas_WhenDirectoryDoesNotExist_ReturnsResult()
    {
        var app = WebApplication.CreateBuilder().Build();

        var result = app.MapOpenApiSchemas(schemasDirectory: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Schemas, Is.Empty);
    }

    [Test]
    public void MapOpenApiSchemas_WhenDirectoryIsEmpty_ReturnsResultWithNoSchemas()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var app = WebApplication.CreateBuilder().Build();

            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Schemas, Is.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_ReturnValueCanBeIgnored()
    {
        // Calling MapOpenApiSchemas() and discarding the return value must remain valid.
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '1.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();

            Assert.DoesNotThrow(() => app.MapOpenApiSchemas(schemasDirectory: tempDir));
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

            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            // Verify an endpoint descriptor was returned for the discovered schema file.
            // HTTP-level assertions (status code, content-type, body) are covered by
            // the integration test GetOpenApiSchema_ReturnsYamlWithCorrectContentType.
            Assert.That(result.Schemas, Has.Count.EqualTo(1));
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

            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(result.Schemas, Has.Count.EqualTo(1));
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

            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(result.Schemas, Has.Count.EqualTo(1));
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

            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(result.Schemas, Is.Empty);
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
            var result = app.MapOpenApiSchemas(schemasDirectory: emptyDir);

            Assert.That(result.Schemas, Is.Not.Empty,
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
            // files take precedence, so only 1 descriptor (from the registered file) is returned.
            File.WriteAllText(Path.Combine(tempDir, "extra.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: Extra\n  version: '9.9.9'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            Assert.That(result.Schemas, Has.Count.EqualTo(1));
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
    public void MapOpenApiSchemas_WithPublishPathOverride_ReturnsDescriptorWithHasOverrideTrue()
    {
        // A registered file with a PublishPathOverride should be mapped directly on the
        // root builder (bypassing the group prefix) and reported in the result with HasOverride = true.
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/myapi.yaml",
            "/contracts/public/v1/openapi.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas, Has.Count.EqualTo(1));
        Assert.That(result.Schemas[0].HasOverride, Is.True,
            "Descriptor for an override-routed schema must have HasOverride = true.");
        Assert.That(result.Schemas[0].PublicPath, Is.EqualTo("/contracts/public/v1/openapi.yaml"),
            "PublicPath must equal the verbatim PublishPathOverride.");
        Assert.That(result.Schemas[0].Endpoint, Is.Not.Null,
            "Endpoint must be a non-null RouteHandlerBuilder.");
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
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas, Has.Count.EqualTo(2));
        Assert.That(result.Schemas.Count(s => !s.HasOverride), Is.EqualTo(1),
            "One descriptor should be for a default-routed schema.");
        Assert.That(result.Schemas.Count(s => s.HasOverride), Is.EqualTo(1),
            "One descriptor should be for an override-routed schema.");
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

    // -------------------------------------------------------------------------
    // Tests for OpenApiSchemaMapResult descriptor content
    // -------------------------------------------------------------------------

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorHasExpectedPublicPath()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '2.5.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(prefix: "/.openapi", schemasDirectory: tempDir);

            Assert.That(result.Schemas, Has.Count.EqualTo(1));
            var descriptor = result.Schemas[0];
            Assert.That(descriptor.PublicPath, Is.EqualTo("/.openapi/schemas/2.5.0/myapi.yaml"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_NoVersion_DescriptorPublicPathOmitsVersionSegment()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "noversion.yaml"), "openapi: '3.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(prefix: "/.openapi", schemasDirectory: tempDir);

            Assert.That(result.Schemas, Has.Count.EqualTo(1));
            var descriptor = result.Schemas[0];
            Assert.That(descriptor.PublicPath, Is.EqualTo("/.openapi/schemas/noversion.yaml"));
            Assert.That(descriptor.Version, Is.Null);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorNameUsesTitleAndVersion()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '1.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            var descriptor = result.Schemas[0];
            Assert.That(descriptor.Name, Is.EqualTo("My API 1.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorNameUsesTitleOnly_WhenNoVersion()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            var descriptor = result.Schemas[0];
            Assert.That(descriptor.Name, Is.EqualTo("My API"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorNameFallsBackToFileName_WhenNoTitle()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            var descriptor = result.Schemas[0];
            Assert.That(descriptor.Name, Is.EqualTo("myapi"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorHasExpectedVersion()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '3.1.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            var descriptor = result.Schemas[0];
            Assert.That(descriptor.Version, Is.EqualTo("3.1.0"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorHasOverrideFalse()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '1.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            var descriptor = result.Schemas[0];
            Assert.That(descriptor.HasOverride, Is.False);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_DefaultRoute_DescriptorEndpointIsNotNull()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '1.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            var descriptor = result.Schemas[0];
            Assert.That(descriptor.Endpoint, Is.Not.Null,
                "Endpoint must be a non-null RouteHandlerBuilder.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_OverrideRoute_DescriptorPublicPathMatchesOverride()
    {
        const string overridePath = "/api/spec/v2/openapi.yaml";
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/999999999/myapi.yaml",
            overridePath);

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas, Has.Count.EqualTo(1));
        Assert.That(result.Schemas[0].PublicPath, Is.EqualTo(overridePath));
    }

    [Test]
    public void MapOpenApiSchemas_OverrideRoute_DescriptorHasOverrideTrue()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/999999999/myapi.yaml",
            "/api/spec/v2/openapi.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas[0].HasOverride, Is.True);
    }

    [Test]
    public void MapOpenApiSchemas_OverrideRoute_DescriptorEndpointIsNotNull()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/999999999/myapi.yaml",
            "/api/spec/v2/openapi.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas[0].Endpoint, Is.Not.Null,
            "Endpoint must be a non-null RouteHandlerBuilder even for override-routed schemas.");
    }

    [Test]
    public void MapOpenApiSchemas_DescriptorEndpoint_CanBeConfiguredWithFluentApi()
    {
        // Demonstrates that the returned RouteHandlerBuilder can be used for fluent configuration.
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '1.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(schemasDirectory: tempDir);

            // Verify the RouteHandlerBuilder fluent API is accessible (does not throw).
            Assert.DoesNotThrow(() => result.Schemas[0].Endpoint.WithName("MyApiSchema"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void MapOpenApiSchemas_CustomPrefix_DescriptorPublicPathUsesPrefix()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "myapi.yaml"),
                "openapi: '3.0.0'\ninfo:\n  title: My API\n  version: '1.0.0'\npaths: {}");

            var app = WebApplication.CreateBuilder().Build();
            var result = app.MapOpenApiSchemas(prefix: "/docs", schemasDirectory: tempDir);

            Assert.That(result.Schemas[0].PublicPath, Does.StartWith("/docs/"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}