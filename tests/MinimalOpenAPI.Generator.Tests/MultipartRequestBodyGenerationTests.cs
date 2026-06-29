namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Regression tests ensuring that a <c>multipart/form-data</c> request body is parsed
/// correctly (ContentType + Schema preserved) but is NOT yet emitted as a JSON request
/// DTO or body parameter by the generator (pending full upload support in #79).
/// </summary>
[TestFixture]
public class MultipartRequestBodyGenerationTests
{
    private static readonly (string, string)[] YamlAdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.UploadFileYaml)
    ];

    private static readonly (string, string)[] JsonAdditionalFiles =
    [
        ("openapi.json", OpenApiFixtures.UploadFileJson)
    ];

    private const string HandlerImpl = """
        public class UploadFileHandler : UploadFileEndpointBase
        {
            public override System.Threading.Tasks.Task<object> HandleAsync(
                System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
        """;

    // ── YAML spec ─────────────────────────────────────────────────────────

    [Test]
    public void Yaml_HandlerBaseClass_DoesNotIncludeRequestBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("request"),
            "Handler base class must not expose a 'request' parameter for multipart bodies");
    }

    [Test]
    public void Yaml_HandlerBaseClass_DoesNotEmitRequestDto()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("sealed record Request"),
            "No inline Request record should be emitted for a multipart body");
    }

    [Test]
    public void Yaml_EndpointMapping_DoesNotIncludeRequestBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Not.Contain("request"),
            "Endpoint mapping lambda must not include a 'request' parameter for multipart bodies");
    }

    [Test]
    public void Yaml_GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public class UploadFileEndpointBase"),
            "Handler base class should still be generated for multipart operations");
    }

    [Test]
    public void Yaml_GeneratesEndpointMapping()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("MapPost("),
            "Endpoint mapping should still be generated for multipart operations");
        Assert.That(source, Does.Contain("/uploads"));
    }

    // ── JSON spec ─────────────────────────────────────────────────────────

    [Test]
    public void Json_HandlerBaseClass_DoesNotIncludeRequestBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: JsonAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("request"),
            "Handler base class must not expose a 'request' parameter for multipart bodies");
    }

    [Test]
    public void Json_EndpointMapping_DoesNotIncludeRequestBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: HandlerImpl,
            additionalFiles: JsonAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Not.Contain("request"),
            "Endpoint mapping lambda must not include a 'request' parameter for multipart bodies");
    }
}