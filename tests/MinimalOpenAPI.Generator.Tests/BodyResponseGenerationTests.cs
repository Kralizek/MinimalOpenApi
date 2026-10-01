namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class BodyResponseGenerationTests
{
    [TestCase(200, "Ok")]
    [TestCase(201, "Created")]
    [TestCase(202, "Accepted")]
    [TestCase(400, "BadRequest")]
    [TestCase(404, "NotFound")]
    [TestCase(409, "Conflict")]
    [TestCase(422, "UnprocessableEntity")]
    [TestCase(500, "InternalServerError")]
    public void JsonBody_UsesStatusSpecificTypedResult(int statusCode, string resultName)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", ResponseYaml.Replace("STATUS", statusCode.ToString(), StringComparison.Ordinal))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResultEndpointBase.g.cs");

        Assert.That(source, Does.Contain($"HttpResults.{resultName}<string>"));
        if (statusCode != 200)
            Assert.That(source, Does.Not.Contain("HttpResults.Ok<string>"));
    }

    [TestCase(204)]
    [TestCase(302)]
    [TestCase(401)]
    [TestCase(403)]
    [TestCase(418)]
    public void JsonBodyWithoutCompatibleTypedResult_UsesIResult(int statusCode)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", ResponseYaml.Replace("STATUS", statusCode.ToString(), StringComparison.Ordinal))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResultEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync("));
        Assert.That(source, Does.Not.Contain("HttpResults.Ok<string>"));
    }

    [TestCase(401, "UnauthorizedHttpResult")]
    [TestCase(403, "ForbidHttpResult")]
    [TestCase(404, "NotFound")]
    [TestCase(409, "Conflict")]
    [TestCase(422, "UnprocessableEntity")]
    public void ResponseWithoutBody_KeepsExistingTypedResult(int statusCode, string resultName)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", SchemaLessResponseYaml.Replace("STATUS", statusCode.ToString(), StringComparison.Ordinal))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResultEndpointBase.g.cs");

        Assert.That(source, Does.Contain($"HttpResults.{resultName}> HandleAsync("));
    }

    [Test]
    public void InternalServerErrorWithoutBody_KeepsIResult()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", SchemaLessResponseYaml.Replace("STATUS", "500", StringComparison.Ordinal))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResultEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync("));
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

    private const string SchemaLessResponseYaml = """
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
        """;
}