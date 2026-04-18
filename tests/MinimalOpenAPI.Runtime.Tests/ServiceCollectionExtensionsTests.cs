using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using MinimalOpenAPI;

namespace MinimalOpenAPI.Runtime.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [TearDown]
    public void TearDown() => ServiceCollectionExtensions.ResetForTesting();

    [Test]
    public void AddMinimalOpenApi_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddMinimalOpenApi();
        Assert.That(result, Is.SameAs(services));
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
    public void MapMinimalOpenApiEndpoints_WhenCallbackRegistered_InvokesCallback()
    {
        var app = WebApplication.CreateBuilder().Build();
        var invoked = false;
        ServiceCollectionExtensions.RegisterEndpointMapping((builder, group) => invoked = true);

        var group = app.MapMinimalOpenApiEndpoints();

        Assert.That(invoked, Is.True);
        Assert.That(group, Is.Not.Null);
    }

    [Test]
    public void RegisterSchemaFile_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/123456789/openapi.yaml"));
    }

    [Test]
    public void MapOpenApiSchemas_WhenNoRegisteredFiles_ReturnsEmptyResult()
    {
        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();
        Assert.That(result.Schemas, Is.Empty);
    }

    [Test]
    public void MapOpenApiSchemas_OnlyEntriesWithPublishAs_AreMapped()
    {
        ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/111111111/hidden.yaml");
        ServiceCollectionExtensions.RegisterSchemaFile("openapi/schemas/222222222/public.yaml", "/openapi/public.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas, Has.Count.EqualTo(1));
        Assert.That(result.Schemas[0].PublicPath, Is.EqualTo("/openapi/public.yaml"));
    }

    [Test]
    public void MapOpenApiSchemas_PublicPath_IsExactlyPublishAs()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/myapi.yaml",
            "/contracts/public/v1/openapi.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas(prefix: "/ignored");

        Assert.That(result.Schemas, Has.Count.EqualTo(1));
        Assert.That(result.Schemas[0].PublicPath, Is.EqualTo("/contracts/public/v1/openapi.yaml"));
    }

    [Test]
    public void MapOpenApiSchemas_DisplayNameFallsBackToFileNameWithoutExtension()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/myapi.yaml",
            "/openapi/schema.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas[0].Name, Is.EqualTo("myapi"));
    }

    [Test]
    public void MapOpenApiSchemas_DisplayVersionFallsBackToNull()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/myapi.yaml",
            "/openapi/schema.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas[0].Version, Is.Null);
    }

    [Test]
    public void MapOpenApiSchemas_UsesMetadataDisplayNameAndDisplayVersion()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/openapi.yaml",
            "/openapi/schema.yaml",
            "Todo API",
            "1.0.0");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas[0].Name, Is.EqualTo("Todo API"));
        Assert.That(result.Schemas[0].Version, Is.EqualTo("1.0.0"));
    }

    [Test]
    public void MapOpenApiSchemas_DoesNotDependOnSchemaFileContentForDisplayMetadata()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/non-existent.yaml",
            "/openapi/schema.yaml",
            "Configured Name",
            "Configured Version");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();
        Assert.That(result.Schemas[0].Name, Is.EqualTo("Configured Name"));
        Assert.That(result.Schemas[0].Version, Is.EqualTo("Configured Version"));
    }

    [Test]
    public void MapOpenApiSchemas_WithDuplicatePublishAs_Throws()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/111111111/api-v1.yaml",
            "/contracts/v1/schema.yaml");
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/222222222/api-v2.yaml",
            "/contracts/v1/schema.yaml");

        var app = WebApplication.CreateBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => app.MapOpenApiSchemas());
    }

    [Test]
    public void MapOpenApiSchemas_DescriptorShape_IsStructurallyPreserved()
    {
        ServiceCollectionExtensions.RegisterSchemaFile(
            "openapi/schemas/987654321/openapi.yaml",
            "/openapi/schema.yaml");

        var app = WebApplication.CreateBuilder().Build();
        var result = app.MapOpenApiSchemas();

        Assert.That(result.Schemas[0].HasOverride, Is.True);
        Assert.That(result.Schemas[0].Endpoint, Is.Not.Null);
        Assert.DoesNotThrow(() => result.Schemas[0].Endpoint.WithName("schema"));
    }
}