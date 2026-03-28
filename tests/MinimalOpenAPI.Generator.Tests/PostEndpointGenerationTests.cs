namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for POST endpoint generation (path + request body).</summary>
[TestFixture]
public class PostEndpointGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.CreateClientYaml)
    ];

    [Test]
    public void GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpoint.g.cs");

        Assert.That(source, Does.Contain("public class CreateClientEndpoint"));
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

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpoint.g.cs");

        Assert.That(source, Does.Contain("System.Guid tenantId"));
        Assert.That(source, Does.Contain("CreateClientRequest request"));
        Assert.That(source, Does.Contain("CancellationToken cancellationToken"));
    }

    [Test]
    public void GeneratedHandlerHasCorrectReturnType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpoint.g.cs");

        Assert.That(source, Does.Contain("Created<Client>"));
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
        Assert.That(source, Does.Contain("CreateClientRequest request"));
        Assert.That(source, Does.Contain("WithName(\"createClient\")"));
    }

    [Test]
    public void GeneratesRegistrationCustomizerBase()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpointRegistration.g.cs");

        Assert.That(source, Does.Contain("public abstract class CreateClientEndpointRegistration"));
    }

    [Test]
    public void ReportsDuplicateRegistrationCustomizerDiagnostic()
    {
        var userSource = CreateClientHandlerImpl + """
            public class RegA : CreateClientEndpointRegistration { }
            public class RegB : CreateClientEndpointRegistration { }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d => d.Id == "MOA003"));
    }

    [Test]
    public void DuplicateRegistrationCustomizerDiagnosticHasOpenApiFileLocation()
    {
        var userSource = CreateClientHandlerImpl + """
            public class RegA : CreateClientEndpointRegistration { }
            public class RegB : CreateClientEndpointRegistration { }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        var moa003 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA003");
        Assert.That(moa003, Is.Not.Null, "MOA003 should be emitted");
        Assert.That(moa003!.Location, Is.Not.EqualTo(Microsoft.CodeAnalysis.Location.None),
            "MOA003 must carry a source location so it is visible in the IDE Error List");
        Assert.That(moa003.Location.GetLineSpan().Path,
            Does.EndWith("openapi.yaml"),
            "MOA003 location should point to the OpenAPI spec file");
    }

    private const string CreateClientHandlerImpl = """
        public class CreateClientHandler : CreateClientEndpoint
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, object request,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}
