namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for GET endpoint generation from a JSON OpenAPI spec.</summary>
[TestFixture]
public class GetEndpointGenerationJsonTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.json", OpenApiFixtures.GetClientJson)
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
    public void MissingHandlerDiagnosticHasOpenApiFileLocation()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        var moa001 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA001");
        Assert.That(moa001, Is.Not.Null, "MOA001 should be emitted");
        Assert.That(moa001!.Location.GetLineSpan().Path,
            Does.EndWith("openapi.json"),
            "MOA001 location should point to the OpenAPI spec file");
    }

    private const string GetClientHandlerImpl = """
        public class GetClientHandler : GetClientEndpointBase
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}
