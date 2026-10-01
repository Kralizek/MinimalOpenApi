namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class BodyResponseGenerationTests
{
    [TestCase(400, "BadRequest")]
    [TestCase(404, "NotFound")]
    [TestCase(409, "Conflict")]
    [TestCase(422, "UnprocessableEntity")]
    public void JsonErrorBody_UsesStatusSpecificTypedResult(int statusCode, string resultName)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", ResponseYaml.Replace("STATUS", statusCode.ToString(), StringComparison.Ordinal))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResultEndpointBase.g.cs");

        Assert.That(source, Does.Contain($"HttpResults.{resultName}<string>"));
        Assert.That(source, Does.Not.Contain("HttpResults.Ok<string>"));
    }

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(500)]
    [TestCase(418)]
    public void JsonBodyWithoutCompatibleTypedResult_UsesIResult(int statusCode)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", ResponseYaml.Replace("STATUS", statusCode.ToString(), StringComparison.Ordinal))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResultEndpointBase.g.cs");

        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.IResult HandleAsync("));
        Assert.That(source, Does.Not.Contain("HttpResults.Ok<string>"));
    }

    private const string ResponseYaml = """
        openapi: "3.0.0"
        info:
          title: Test
          version: "1.0"
        paths:
          /result:
            get:
              operationId: getResult
              responses:
                "STATUS":
                  description: Response
                  content:
                    application/json:
                      schema:
                        type: string
        """;
}
