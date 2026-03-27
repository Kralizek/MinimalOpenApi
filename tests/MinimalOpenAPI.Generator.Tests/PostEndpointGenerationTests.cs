using FluentAssertions;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for POST endpoint generation (path + request body).</summary>
public class PostEndpointGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.CreateClientYaml)
    ];

    [Fact]
    public void GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpoint.g.cs");

        source.Should().Contain("public abstract class CreateClientEndpoint");
    }

    [Fact]
    public void GeneratedHandlerIncludesBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpoint.g.cs");

        source.Should().Contain("System.Guid tenantId");
        source.Should().Contain("CreateClientRequest request");
        source.Should().Contain("CancellationToken cancellationToken");
    }

    [Fact]
    public void GeneratedHandlerHasCorrectReturnType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpoint.g.cs");

        source.Should().Contain("Created<Client>");
        source.Should().Contain("BadRequest");
    }

    [Fact]
    public void GeneratesDtosForRequestAndResponseSchemas()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        source.Should().Contain("public sealed record CreateClientRequest(");
        source.Should().Contain("public sealed record Client(");
    }

    [Fact]
    public void GeneratesEndpointMappingWithBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        source.Should().Contain("MapPost(");
        source.Should().Contain("/tenants/{tenantId:guid}/clients");
        source.Should().Contain("CreateClientRequest request");
        source.Should().Contain("WithName(\"createClient\")");
    }

    [Fact]
    public void GeneratesRegistrationCustomizerBase()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateClientEndpointRegistration.g.cs");

        source.Should().Contain("public abstract class CreateClientEndpointRegistration");
    }

    [Fact]
    public void ReportsDuplicateRegistrationCustomizerDiagnostic()
    {
        var userSource = CreateClientHandlerImpl + """
            public class RegA : CreateClientEndpointRegistration { }
            public class RegB : CreateClientEndpointRegistration { }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        var diagnostics = result.Diagnostics;
        diagnostics.Should().Contain(d => d.Id == "MOA003");
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
