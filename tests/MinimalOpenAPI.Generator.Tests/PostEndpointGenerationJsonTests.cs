namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for POST endpoint generation from a JSON OpenAPI spec.</summary>
[TestFixture]
public class PostEndpointGenerationJsonTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.json", OpenApiFixtures.CreateClientJson)
    ];

    [Test]
    public void GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public class CreateClientEndpointBase"));
        Assert.That(source, Does.Contain("public virtual"));
        Assert.That(source, Does.Contain("HandleAsync("));
        Assert.That(source, Does.Contain("NotImplementedException"));
    }

    [Test]
    public void GeneratedHandlerIncludesBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("System.Guid tenantId"));
        Assert.That(source, Does.Contain("global::TestProject.Contracts.CreateClientRequest request"));
        Assert.That(source, Does.Contain("CancellationToken cancellationToken"));
    }

    [Test]
    public void GeneratedHandlerHasCorrectReturnType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Created<global::TestProject.Contracts.Client>"));
        Assert.That(source, Does.Contain("BadRequest"));
    }

    [Test]
    public void GeneratesDtosForRequestAndResponseSchemas()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record CreateClientRequest"));
        Assert.That(source, Does.Contain("public sealed record Client"));
        Assert.That(source, Does.Contain("[JsonPropertyName("));
        Assert.That(source, Does.Contain("{ get; init; }"));
    }

    [Test]
    public void GeneratesEndpointMappingWithBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("MapPost("));
        Assert.That(source, Does.Contain("/tenants/{tenantId:guid}/clients"));
        Assert.That(source, Does.Contain("global::TestProject.Contracts.CreateClientRequest request"));
        Assert.That(source, Does.Contain("WithName(\"createClient\")"));
    }

    private const string CreateClientHandlerImpl = """
        public class CreateClientHandler : CreateClientEndpointBase
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, object request,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}