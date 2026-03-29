namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for GET endpoint generation (path + query parameters).</summary>
[TestFixture]
public class GetEndpointGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.GetClientYaml)
    ];

    [Test]
    public void GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public class GetClientEndpointBase"));
        Assert.That(source, Does.Contain("public virtual"));
        Assert.That(source, Does.Contain("HandleAsync("));
        Assert.That(source, Does.Contain("NotImplementedException"));
    }

    [Test]
    public void GeneratedHandlerHasCorrectHandleSignature()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("System.Guid tenantId"));
        Assert.That(source, Does.Contain("System.Guid clientId"));
        Assert.That(source, Does.Contain("bool? includeDeleted"));
        Assert.That(source, Does.Contain("CancellationToken cancellationToken"));
        Assert.That(source, Does.Contain("HandleAsync("));
    }

    [Test]
    public void GeneratedHandlerHasCorrectReturnType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Results<"));
        Assert.That(source, Does.Contain("Ok<global::TestProject.Contracts.Client>"));
        Assert.That(source, Does.Contain("NotFound>"));
    }

    [Test]
    public void GeneratesRegistrationCustomizerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointRegistration.g.cs");

        Assert.That(source, Does.Contain("public abstract class GetClientEndpointRegistration"));
        Assert.That(source, Does.Contain("Configure("));
        Assert.That(source, Does.Contain("RouteHandlerBuilder builder"));
    }

    [Test]
    public void GeneratesDtoForClientSchema()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record Client"));
        Assert.That(source, Does.Contain("public global::System.Guid Id { get; init; }"));
        Assert.That(source, Does.Contain("public string Name { get; init; }"));
        Assert.That(source, Does.Contain("public string? VatNumber { get; init; }"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"id\")]"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"name\")]"));
    }

    [Test]
    public void GeneratesEndpointMappingWithRouteConstraints()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("/tenants/{tenantId:guid}/clients/{clientId:guid}"));
        Assert.That(source, Does.Contain("MapGet("));
        Assert.That(source, Does.Contain("WithName(\"getClient\")"));
        // The generated class is internal; the public MapMinimalOpenApiEndpoints lives in the runtime.
        Assert.That(source, Does.Contain("internal static class MinimalOpenApiGeneratedEndpointRouteBuilderExtensions"));
        Assert.That(source, Does.Contain("internal static"));
        Assert.That(source, Does.Contain("MapEndpoints("));
    }

    [Test]
    public void GeneratesEndpointMappingWithSummaryDescriptionAndTags()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain(".WithSummary(\"Get a specific client\")"));
        Assert.That(source, Does.Contain(".WithDescription(\"Returns the client with the specified identifier.\")"));
        Assert.That(source, Does.Contain(".WithTags(\"clients\")"));
    }

    [Test]
    public void GeneratesDiRegistration()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Contain("AddGeneratedEndpoints("));
        Assert.That(source, Does.Contain("GetClientEndpointBase"));
        Assert.That(source, Does.Contain("GetClientHandler"));
        // ModuleInitializer must also register the endpoint mapping delegate.
        Assert.That(source, Does.Contain("RegisterEndpointMapping("));
        Assert.That(source, Does.Contain("MapEndpoints("));
    }

    [Test]
    public void ReportsMissingHandlerImplementationDiagnostic()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA001" &&
            d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Test]
    public void MissingHandlerDiagnosticIncludesFullyQualifiedTypeName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        var moa001 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA001");
        Assert.That(moa001, Is.Not.Null, "MOA001 should be emitted");
        Assert.That(moa001!.GetMessage(),
            Does.Contain("TestProject.Endpoints.GetClientEndpointBase"),
            "MOA001 message must include the fully-qualified type name so users know exactly what to inherit from");
    }

    [Test]
    public void MissingHandlerDiagnosticHasOpenApiFileLocation()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        var moa001 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA001");
        Assert.That(moa001, Is.Not.Null, "MOA001 should be emitted");
        Assert.That(moa001!.Location, Is.Not.EqualTo(Microsoft.CodeAnalysis.Location.None),
            "MOA001 must carry a source location so it is visible in the IDE Error List");
        Assert.That(moa001.Location.GetLineSpan().Path,
            Does.EndWith("openapi.yaml"),
            "MOA001 location should point to the OpenAPI spec file");
    }

    [Test]
    public void ReportsDuplicateHandlerImplementationDiagnostic()
    {
        var userSource = """
            public class HandlerA : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            public class HandlerB : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d => d.Id == "MOA002"));
    }

    [Test]
    public void DuplicateHandlerDiagnosticHasOpenApiFileLocation()
    {
        var userSource = """
            public class HandlerA : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            public class HandlerB : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        var moa002 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA002");
        Assert.That(moa002, Is.Not.Null, "MOA002 should be emitted");
        Assert.That(moa002!.Location, Is.Not.EqualTo(Microsoft.CodeAnalysis.Location.None),
            "MOA002 must carry a source location so it is visible in the IDE Error List");
        Assert.That(moa002.Location.GetLineSpan().Path,
            Does.EndWith("openapi.yaml"),
            "MOA002 location should point to the OpenAPI spec file");
    }

    // A minimal implementation for tests that need exactly one handler
    private const string GetClientHandlerImpl = """
        public class GetClientHandler : GetClientEndpointBase
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}
