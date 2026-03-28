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

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpoint.g.cs");

        Assert.That(source, Does.Contain("public class GetClientEndpoint"));
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

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpoint.g.cs");

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

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpoint.g.cs");

        Assert.That(source, Does.Contain("Results<"));
        Assert.That(source, Does.Contain("Ok<Client>"));
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
        Assert.That(source, Does.Contain("MapMinimalOpenApiEndpoints("));
    }

    [Test]
    public void GeneratesDiRegistration()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Contain("AddGeneratedEndpoints("));
        Assert.That(source, Does.Contain("GetClientEndpoint"));
        Assert.That(source, Does.Contain("GetClientHandler"));
    }

    [Test]
    public void ReportsMissingHandlerImplementationDiagnostic()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d => d.Id == "MOA001" && d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Test]
    public void ReportsDuplicateHandlerImplementationDiagnostic()
    {
        var userSource = """
            public class HandlerA : GetClientEndpoint
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            public class HandlerB : GetClientEndpoint
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

    // A minimal implementation for tests that need exactly one handler
    private const string GetClientHandlerImpl = """
        public class GetClientHandler : GetClientEndpoint
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}
